using Microsoft.AspNetCore.Mvc;
using Client.Models;
using Microsoft.AspNetCore.Authentication;
using System.Threading.Tasks;
using System.Net.Http;
using System.Text;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Logging;
using System.Net.Mime;
using Microsoft.AspNetCore.Http;

namespace Client.Controllers
{    [Authorize]
    [Route("[controller]")]
    public class UserProfileController : Controller
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<UserProfileController> _logger;
        private readonly IHttpClientFactory _httpClientFactory;

        public UserProfileController(HttpClient httpClient, IConfiguration configuration, ILogger<UserProfileController> logger, IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
            _httpClientFactory = httpClientFactory;        }

        [HttpGet]
        public IActionResult Index()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("User ID not found in token.");
            }

            if (int.TryParse(userId, out int id))
            {
                return RedirectToAction(nameof(Index), new { id });
            }
            
            return BadRequest("Invalid user ID format.");
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Index(int id)
        {
            var token = User.FindFirst("JWT")?.Value;
            if (string.IsNullOrEmpty(token))
                return Unauthorized();

            var client = _httpClientFactory.CreateClient("API");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            // Get user profile
            var userResponse = await client.GetAsync($"api/UserProfile/{id}");
            if (!userResponse.IsSuccessStatusCode)
                return NotFound();
            
            var userJson = await userResponse.Content.ReadAsStringAsync();
            var user = JsonConvert.DeserializeObject<UserViewModel>(userJson);
            
            // Get user posts
            var postsResponse = await client.GetAsync($"api/UserProfile/{id}/posts");
            List<PostViewModel> posts = new List<PostViewModel>();
            if (postsResponse.IsSuccessStatusCode)
            {
                var postsJson = await postsResponse.Content.ReadAsStringAsync();
                var jsonObject = JsonConvert.DeserializeObject<List<dynamic>>(postsJson);
                
                posts = jsonObject?.Select(p => new PostViewModel
                {
                    PostID = Convert.ToInt32(p.postID ?? 0),
                    Title = (string?)p.title ?? string.Empty,
                    Content = (string?)p.content ?? string.Empty,
                    CreatedAt = (DateTime?)p.createdAt ?? DateTime.UtcNow,
                    UserID = Convert.ToInt32(p.userID ?? 0),
                    Username = (string?)p.userName ?? string.Empty,
                    CategoryName = (string?)p.categoryName,
                    LikesCount = Convert.ToInt32(p.likesCount ?? 0),
                    CommentsCount = Convert.ToInt32(p.commentsCount ?? 0),
                    ImageURL = p.mediaFiles != null ? 
                        ((dynamic[])p.mediaFiles)
                            .FirstOrDefault(m => (string?)m.type == "image")?.url as string 
                        : null,
                    VideoURL = p.mediaFiles != null ? 
                        ((dynamic[])p.mediaFiles)
                            .FirstOrDefault(m => (string?)m.type == "video")?.url as string 
                        : null
                }).ToList() ?? new List<PostViewModel>();
            }

            user.Posts = posts;
            return View(user);
        }

        [HttpGet("edit")]
        public async Task<IActionResult> Edit()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("User ID not found in token.");
            }

            var token = User.FindFirst("JWT")?.Value;
            if (string.IsNullOrEmpty(token))
            {
                return Unauthorized("JWT token not found.");
            }

            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.GetAsync($"api/UserProfile/{userId}");

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var userProfile = JsonConvert.DeserializeObject<UserViewModel>(content);
                
                var profileViewModel = new ProfileViewModel
                {
                    Username = userProfile.Username,
                    FullName = userProfile.FullName,
                    Email = userProfile.Email,
                    Bio = userProfile.Bio,
                    ProfileImageURL = userProfile.ProfileImageURL,
                    TalentArea = userProfile.TalentArea
                };
                return View(profileViewModel);
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return NotFound("User profile not found.");
            }
            else
            {
                ModelState.AddModelError("", "Error fetching user profile for editing.");
                return View(new ProfileViewModel());
            }
        }

        [HttpPost("edit")]
        public async Task<IActionResult> Edit(ProfileViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("User ID not found in token.");
            }

            var token = User.FindFirst("JWT")?.Value;
            if (string.IsNullOrEmpty(token))
            {
                return Unauthorized("JWT token not found.");
            }

            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            using var formData = new MultipartFormDataContent();

            formData.Add(new StringContent(model.Username ?? string.Empty), "Username");
            formData.Add(new StringContent(model.FullName ?? string.Empty), "FullName");
            formData.Add(new StringContent(model.Email ?? string.Empty), "Email");
            formData.Add(new StringContent(model.Bio ?? string.Empty), "Bio");
            formData.Add(new StringContent(model.TalentArea ?? string.Empty), "TalentArea");
            formData.Add(new StringContent(model.ProfilePrivacy.ToString()), "ProfilePrivacy");
            
            if (!string.IsNullOrEmpty(model.ProfileImageURL) && model.ProfileImageFile == null)
            {
                formData.Add(new StringContent(model.ProfileImageURL), "ProfileImageUrl");
            }

            if (model.ProfileImageFile != null)
            {
                var fileStreamContent = new StreamContent(model.ProfileImageFile.OpenReadStream());
                fileStreamContent.Headers.ContentType = new MediaTypeHeaderValue(model.ProfileImageFile.ContentType);
                formData.Add(fileStreamContent, "ProfileImageFile", model.ProfileImageFile.FileName);
            }

            var response = await _httpClient.PutAsync($"api/UserProfile/{userId}", formData);

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var updatedUser = JsonConvert.DeserializeObject<UserViewModel>(responseContent);

                var identity = new ClaimsIdentity(User.Claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var existingClaim = identity.FindFirst("ProfileImageURL");
                if (existingClaim != null)
                {
                    identity.RemoveClaim(existingClaim);
                }
                identity.AddClaim(new Claim("ProfileImageURL", updatedUser.ProfileImageURL ?? "https://via.placeholder.com/30/007bff/FFFFFF?text=U"));

                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));

                TempData["SuccessMessage"] = "Profile updated successfully!";
                return RedirectToAction("Index");
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                ModelState.AddModelError("", $"Error updating profile: {errorContent}");
                return View(model);
            }
        }

        [HttpGet("verify-password")]
        public IActionResult VerifyCurrentPassword()
        {
            return View();
        }

        [HttpPost("verify-password")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyCurrentPassword(VerifyCurrentPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    TempData["Error"] = "User not authenticated.";
                    return RedirectToAction("Index");
                }

                var token = User.FindFirst("JWT")?.Value;
                if (string.IsNullOrEmpty(token))
                {
                    TempData["Error"] = "Authentication token not found.";
                    return RedirectToAction("Index");
                }

                var client = _httpClientFactory.CreateClient("API");
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var verifyResponse = await client.PostAsJsonAsync("api/Auth/login", new
                {
                    Email = User.FindFirst(ClaimTypes.Email)?.Value,
                    Password = model.CurrentPassword
                });

                if (verifyResponse.IsSuccessStatusCode)
                {
                    HttpContext.Session.SetString("PasswordVerified", "true");
                    HttpContext.Session.SetString("VerifiedCurrentPassword", model.CurrentPassword);
                    return RedirectToAction("ChangePassword");
                }
                else
                {
                    ModelState.AddModelError("", "Current password is incorrect.");
                    return View(model);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying current password");
                ModelState.AddModelError("", "An error occurred while verifying your password.");
                return View(model);
            }
        }

        [HttpGet("change-password")]
        public IActionResult ChangePassword()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("PasswordVerified")))
            {
                return RedirectToAction("VerifyCurrentPassword");
            }

            return View();
        }

        [HttpPost("change-password")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("PasswordVerified")))
            {
                return RedirectToAction("VerifyCurrentPassword");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    TempData["Error"] = "User not authenticated.";
                    return RedirectToAction("Index");
                }

                var token = User.FindFirst("JWT")?.Value;
                if (string.IsNullOrEmpty(token))
                {
                    TempData["Error"] = "Authentication token not found.";
                    return RedirectToAction("Index");
                }

                var client = _httpClientFactory.CreateClient("API");
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var currentPassword = HttpContext.Session.GetString("VerifiedCurrentPassword");
                if (string.IsNullOrEmpty(currentPassword))
                {
                    return RedirectToAction("VerifyCurrentPassword");
                }

                var response = await client.PostAsJsonAsync("api/UserProfile/ChangePassword", new
                {
                    CurrentPassword = currentPassword,
                    NewPassword = model.NewPassword
                });

                if (response.IsSuccessStatusCode)
                {
                    TempData["PasswordChangeSuccess"] = true;
                    HttpContext.Session.Remove("PasswordVerified");
                    HttpContext.Session.Remove("VerifiedCurrentPassword");
                    return View(model);
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    ModelState.AddModelError("", "Failed to change password. Please try again.");
                    return View(model);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing password");
                ModelState.AddModelError("", "An error occurred while changing your password.");
                return View(model);
            }
        }
    }
}
