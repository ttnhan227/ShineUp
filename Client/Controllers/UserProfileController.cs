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
{
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
            // Redirect to the public profile page of the currently logged-in user
            var username = User.Identity?.Name; // Assumes username is stored in User.Identity.Name
            if (string.IsNullOrEmpty(username))
            {
                // If not authenticated or username is not available, redirect to login or home
                // For now, let's redirect to login if not authenticated, or home if username is missing
                if (User.Identity?.IsAuthenticated != true)
                {
                    return RedirectToAction("Login", "Auth");
                }
                _logger.LogWarning("UserProfile/Index: Username not found in claims for authenticated user. Redirecting to Home/Index.");
                return RedirectToAction("Index", "Home");
            }
            return RedirectToAction(nameof(PublicProfile), new { username });
        }

        // Removed Index(int id) action as it's no longer used.

        [AllowAnonymous] // Allow anonymous access for username-based profiles
        [HttpGet("{username}")] // Route for username (string)
        public async Task<IActionResult> PublicProfile(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                return BadRequest("Username cannot be empty.");
            }

            var client = _httpClientFactory.CreateClient("API");
            // No Authorization header for public profiles

            // Get user profile by username
            var userResponse = await client.GetAsync($"api/UserProfile/username/{username}");
            if (!userResponse.IsSuccessStatusCode)
            {
                _logger.LogError($"Failed to get user profile for username {username}. Status: {userResponse.StatusCode}");
                if (userResponse.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return NotFound($"Profile for '{username}' not found.");
                }
                return StatusCode((int)userResponse.StatusCode, "Error fetching profile.");
            }
            
            var userJson = await userResponse.Content.ReadAsStringAsync();
            var user = JsonConvert.DeserializeObject<UserViewModel>(userJson);

            if (user == null) // Double check after deserialization
            {
                _logger.LogError($"User profile for username {username} deserialized to null.");
                return NotFound($"Profile for '{username}' not found.");
            }
            
            // Get user posts by user ID (since the server endpoint for posts still uses ID)
            // We need the UserID from the profile we just fetched.
            // Get user posts by username directly from the new server endpoint
            var postsResponse = await client.GetAsync($"api/UserProfile/username/{username}/posts");
            List<PostDetailsViewModel> posts = new List<PostDetailsViewModel>();
            if (postsResponse.IsSuccessStatusCode)
            {
                var postsJson = await postsResponse.Content.ReadAsStringAsync();
                posts = System.Text.Json.JsonSerializer.Deserialize<List<PostDetailsViewModel>>(postsJson, new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }) ?? new List<PostDetailsViewModel>();
            }
            else
            {
                 _logger.LogWarning($"Failed to get posts for user {username} (ID: {user.UserID}). Status: {postsResponse.StatusCode}");
            }

            user.Posts = posts;
            // We can reuse the same Index.cshtml view if it's suitable for public display
            // or create a new PublicProfile.cshtml if needed.
            // For now, let's assume Index.cshtml can be used.
            return View("Index", user);
        }

        [Authorize] // Keep Authorize for edit
        [HttpGet("edit")]
        public async Task<IActionResult> Edit()
        {
            var username = User.Identity?.Name; // Assumes username is stored in User.Identity.Name
            if (string.IsNullOrEmpty(username))
            {
                _logger.LogWarning("Edit GET: Username not found in claims for authenticated user.");
                return Unauthorized("Username not found in token.");
            }

            var token = User.FindFirst("JWT")?.Value;
            if (string.IsNullOrEmpty(token))
            {
                return Unauthorized("JWT token not found.");
            }

            var client = _httpClientFactory.CreateClient("API");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            // Fetch profile by username for editing
            var response = await client.GetAsync($"api/UserProfile/username/{username}");

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

            // Username for redirection, assuming it might be part of the model or can be fetched again if needed.
            // For simplicity, we'll use model.Username for redirection.
            // var currentUsernameForRedirect = User.Identity?.Name;

            var token = User.FindFirst("JWT")?.Value;
            if (string.IsNullOrEmpty(token))
            {
                return Unauthorized("JWT token not found.");
            }
            
            var client = _httpClientFactory.CreateClient("API");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

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

            // UserID is not part of the URL anymore, server gets it from token
            var response = await client.PutAsync("api/UserProfile/update", formData);

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
                // Redirect to the public profile page using the username from the model
                return RedirectToAction(nameof(PublicProfile), new { username = model.Username });
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
                    // If user ID is not found, perhaps redirect to login or an error page
                    return RedirectToAction("Login", "Auth");
                }

                var token = User.FindFirst("JWT")?.Value;
                if (string.IsNullOrEmpty(token))
                {
                    TempData["Error"] = "Authentication token not found.";
                    return RedirectToAction("Login", "Auth");
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
                    // If user ID is not found, perhaps redirect to login or an error page
                    return RedirectToAction("Login", "Auth");
                }

                var token = User.FindFirst("JWT")?.Value;
                if (string.IsNullOrEmpty(token))
                {
                    TempData["Error"] = "Authentication token not found.";
                    return RedirectToAction("Login", "Auth");
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
