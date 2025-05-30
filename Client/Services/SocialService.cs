using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Client.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Client.Services
{
    public class SocialService : ISocialService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<SocialService> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public SocialService(
            HttpClient httpClient,
            IConfiguration configuration,
            ILogger<SocialService> logger,
            IHttpContextAccessor httpContextAccessor)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
            
            var baseUrl = _configuration["ApiBaseUrl"];
            if (string.IsNullOrEmpty(baseUrl))
            {
                throw new InvalidOperationException("ApiBaseUrl is not configured in appsettings.json");
            }
            _httpClient.BaseAddress = new Uri(baseUrl);
        }

        private async Task<HttpClient> GetAuthenticatedClientAsync()
        {
            var token = _httpContextAccessor.HttpContext?.Request.Cookies["auth_token"];
            if (string.IsNullOrEmpty(token))
            {
                token = _httpContextAccessor.HttpContext?.Request.Headers["Authorization"].ToString()?.Replace("Bearer ", "");
            }
            
            if (!string.IsNullOrEmpty(token) && !_httpClient.DefaultRequestHeaders.Contains("Authorization"))
            {
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
            
            return _httpClient;
         }

        // Comments
        public async Task<List<CommentViewModel>> GetCommentsForPostAsync(int postId)
        {
            try
            {
                var client = await GetAuthenticatedClientAsync();
                var response = await client.GetAsync($"api/comments/post/{postId}");
                response.EnsureSuccessStatusCode();
                
                var content = await response.Content.ReadAsStringAsync();
                var comments = JsonConvert.DeserializeObject<List<CommentViewModel>>(content);
                return comments ?? new List<CommentViewModel>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting comments for post {PostId}", postId);
                throw;
            }
        }

        public async Task<CommentViewModel> CreateCommentAsync(CommentViewModel comment)
        {
            try
            {
                var client = await GetAuthenticatedClientAsync();
                var response = await client.PostAsJsonAsync("api/comments", new 
                { 
                    PostID = comment.PostID,
                    VideoID = (int?)null, // Set to null since we're only handling post comments for now
                    Content = comment.Content 
                });
                response.EnsureSuccessStatusCode();
                
                return await response.Content.ReadFromJsonAsync<CommentViewModel>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating comment");
                throw;
            }
        }

        public async Task<CommentViewModel> AddCommentAsync(CommentViewModel comment)
        {
            try
            {
                _logger.LogInformation("Adding comment: {@Comment}", comment);
                
                var client = await GetAuthenticatedClientAsync();
                var content = new StringContent(
                    JsonConvert.SerializeObject(new {
                        PostID = comment.PostID,
                        Content = comment.Content,
                        VideoID = (int?)null
                    }), 
                    Encoding.UTF8, 
                    "application/json");
                
                var response = await client.PostAsync("api/comments", content);
                response.EnsureSuccessStatusCode();
                
                var responseContent = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<CommentViewModel>(responseContent);
                _logger.LogInformation("Successfully added comment");
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding comment for post {PostId}", comment?.PostID);
                throw;
            }
        }

        public async Task<bool> DeleteCommentAsync(int commentId)
        {
            try
            {
                var client = await GetAuthenticatedClientAsync();
                var response = await client.DeleteAsync($"api/comments/{commentId}");
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return false; // Comment not found
                }
                response.EnsureSuccessStatusCode();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting comment {CommentId}", commentId);
                throw;
            }
        }

        // Likes
        public async Task<List<LikeViewModel>> GetLikesForPostAsync(int postId)
        {
            try
            {
                var client = await GetAuthenticatedClientAsync();
                var response = await client.GetAsync($"api/likes/post/{postId}");
                response.EnsureSuccessStatusCode();
                
                var content = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<List<LikeViewModel>>(content) ?? new List<LikeViewModel>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting likes for post {PostId}", postId);
                throw;
            }
        }

        public async Task<LikeViewModel> ToggleLikeAsync(CreateLikeViewModel like)
        {
            try
            {
                var client = await GetAuthenticatedClientAsync();
                var content = new StringContent(
                    JsonConvert.SerializeObject(new {
                        PostID = like.PostID,
                        VideoID = (int?)null
                    }), 
                    Encoding.UTF8, 
                    "application/json");
                
                var response = await client.PostAsync("api/likes/toggle", content);
                
                if (response.StatusCode == System.Net.HttpStatusCode.NoContent)
                {
                    return null; // Like was removed
                }
                
                response.EnsureSuccessStatusCode();
                var responseContent = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<LikeViewModel>(responseContent);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling like");
                throw;
            }
        }

        public async Task<bool> HasUserLikedPostAsync(int postId)
        {
            try
            {
                var client = await GetAuthenticatedClientAsync();
                var response = await client.GetAsync($"api/likes/post/{postId}/status");
                response.EnsureSuccessStatusCode();
                
                return await response.Content.ReadFromJsonAsync<bool>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if user liked post {PostId}", postId);
                throw;
            }
        }

        public async Task<bool> HasLikedPostAsync(int postId)
        {
            try
            {
                var client = await GetAuthenticatedClientAsync();
                var response = await client.GetAsync($"api/likes/post/{postId}");
                response.EnsureSuccessStatusCode();
                
                var content = await response.Content.ReadAsStringAsync();
                var likes = JsonConvert.DeserializeObject<List<LikeViewModel>>(content);
                var hasLiked = likes?.Any() == true;
                
                _logger.LogInformation("Successfully checked like status. HasLiked: {HasLiked}", hasLiked);
                return hasLiked;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if post {PostId} is liked", postId);
                throw;
            }
        }

        public async Task<int> GetLikeCountAsync(int postId)
        {
            try
            {
                var client = await GetAuthenticatedClientAsync();
                var response = await client.GetAsync($"api/likes/post/{postId}");
                response.EnsureSuccessStatusCode();
                
                var content = await response.Content.ReadAsStringAsync();
                var likes = JsonConvert.DeserializeObject<List<LikeViewModel>>(content);
                return likes?.Count ?? 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting like count for post {PostId}", postId);
                throw;
            }
        }
    }
}
