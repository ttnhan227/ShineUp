using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Client.Models;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using System.Net.Http.Headers;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http;
using System.Text;

namespace Client.Controllers
{
    [Authorize]
    public class SocialController : Controller
    {
        private readonly IHttpClientFactory _clientFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<SocialController> _logger;

        public SocialController(
            IHttpClientFactory clientFactory,
            IConfiguration configuration,
            ILogger<SocialController> logger)
        {
            _clientFactory = clientFactory;
            _configuration = configuration;
            _logger = logger;
        }

        private async Task<HttpClient> GetAuthenticatedClient()
        {
            var client = _clientFactory.CreateClient("API");
            
            // Check if user is authenticated
            if (!User.Identity.IsAuthenticated)
            {
                _logger.LogWarning("User is not authenticated");
                return null;
            }
            
            // Get the authentication cookie
            var authCookie = Request.Cookies["ShineUpAuth"];
            if (string.IsNullOrEmpty(authCookie))
            {
                _logger.LogWarning("Authentication cookie not found");
                return null;
            }
            
            // Get the JWT token from the claims
            var token = User.FindFirst("JWT")?.Value;
            if (string.IsNullOrEmpty(token))
            {
                _logger.LogWarning("JWT token not found in claims");
                return null;
            }

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            _logger.LogInformation("JWT token added to request");
            
            return client;
        }

        // Add a comment to a post
        [HttpPost("AddComment")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddComment(CreateCommentViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return RedirectToAction("Details", "Posts", new { id = model.PostID });
            }

            try
            {
                var client = await GetAuthenticatedClient();
                var response = await client.PostAsJsonAsync("api/social/comments", model);
                
                if (!response.IsSuccessStatusCode)
                {
                    return RedirectToAction("Details", "Posts", new { id = model.PostID });
                }

                return RedirectToAction("Details", "Posts", new { id = model.PostID });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding comment");
                return RedirectToAction("Details", "Posts", new { id = model.PostID });
            }
        }

        // Delete a comment
        [HttpPost("DeleteComment")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteComment(int commentId, int postId)
        {
            try
            {
                var client = await GetAuthenticatedClient();
                var response = await client.DeleteAsync($"api/social/comments/{commentId}");
                
                if (!response.IsSuccessStatusCode)
                {
                    return RedirectToAction("Details", "Posts", new { id = postId });
                }

                return RedirectToAction("Details", "Posts", new { id = postId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting comment {CommentId}", commentId);
                return RedirectToAction("Details", "Posts", new { id = postId });
            }
        }

        // Toggle like on a post or video
        [HttpPost("ToggleLike")]    
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleLike(ToggleLikeViewModel model)
        {
            if (!ModelState.IsValid || (!model.PostID.HasValue && string.IsNullOrEmpty(model.VideoID)))
            {
                TempData["ErrorMessage"] = "Invalid request";
                // Redirect based on what's available
                if (model.PostID.HasValue)
                    return RedirectToAction("Details", "Posts", new { id = model.PostID });
                return RedirectToAction("Index", "Home");
            }

            try
            {
                var client = await GetAuthenticatedClient();
                if (client == null)
                {
                    var returnUrl = model.PostID.HasValue 
                        ? $"/Posts/Details/{model.PostID}" 
                        : $"/Videos/Details/{model.VideoID}";
                    return RedirectToAction("Login", "Auth", new { returnUrl });
                }

                var response = await client.PostAsJsonAsync("api/social/likes/toggle", model);
                
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    var returnUrl = model.PostID.HasValue 
                        ? $"/Posts/Details/{model.PostID}" 
                        : $"/Videos/Details/{model.VideoID}";
                    return RedirectToAction("Login", "Auth", new { returnUrl });
                }
                else if (!response.IsSuccessStatusCode && response.StatusCode != System.Net.HttpStatusCode.NoContent)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Failed to update like. Status: {StatusCode}, Response: {Response}", 
                        response.StatusCode, errorContent);
                    // Error logged but no user-facing message shown
                }
                else
                {
                    // Like status updated successfully, no message shown
                }

                // Redirect based on what's available
                if (model.PostID.HasValue)
                    return RedirectToAction("Details", "Posts", new { id = model.PostID });
                else if (!string.IsNullOrEmpty(model.VideoID))
                    return RedirectToAction("Details", "Videos", new { id = model.VideoID });
                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling like");
                TempData["ErrorMessage"] = "An error occurred while updating the like";
                // Redirect based on what's available
                if (model.PostID.HasValue)
                    return RedirectToAction("Details", "Posts", new { id = model.PostID });
                else if (!string.IsNullOrEmpty(model.VideoID))
                    return RedirectToAction("Details", "Videos", new { id = model.VideoID });
                return RedirectToAction("Index", "Home");
            }
        }
        [HttpGet]
public async Task<IActionResult> GetComments(int postId)
{
    try
    {
        var client = await GetAuthenticatedClient();
        if (client == null)
        {
            return Unauthorized();
        }

        var response = await client.GetAsync($"api/social/posts/{postId}/comments");
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Failed to get comments for post {PostId}. Status code: {StatusCode}", postId, response.StatusCode);
            return Json(new List<CommentViewModel>());
        }

        var content = await response.Content.ReadAsStringAsync();
        var comments = JsonSerializer.Deserialize<List<CommentViewModel>>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        // Get profile images for commenters
        if (comments != null && comments.Any())
        {
            var userIds = comments.Select(c => c.UserID).Distinct().ToList();
            var usersResponse = await client.PostAsJsonAsync("api/UserProfile/profiles", new { UserIds = userIds });
            if (usersResponse.IsSuccessStatusCode)
            {
                var usersContent = await usersResponse.Content.ReadAsStringAsync();
                var users = JsonSerializer.Deserialize<List<UserViewModel>>(usersContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                var userDictionary = users?.ToDictionary(u => u.UserID, u => u.ProfileImageURL) ?? new Dictionary<int, string>();
                foreach (var comment in comments)
                {
                    if (userDictionary.TryGetValue(comment.UserID, out var profileImageUrl))
                    {
                        comment.ProfileImageURL = profileImageUrl;
                    }
               
                }
            }
        }

        return Json(comments ?? new List<CommentViewModel>());
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error getting comments for post {PostId}", postId);
        return Json(new List<CommentViewModel>());
    }
}
    }
    
}
