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
    [Authorize]
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
            _httpClientFactory = httpClientFactory;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
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
                _logger.LogInformation($"User profile response: {content}");
                var userProfile = JsonConvert.DeserializeObject<UserViewModel>(content);
                _logger.LogInformation($"Deserialized IsGoogleAccount: {userProfile.IsGoogleAccount}");
                return View(userProfile);
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return NotFound("User profile not found.");
            }
            else
            {
                ModelState.AddModelError("", "Error fetching user profile.");
                return View(new UserViewModel()); // Return an empty model or handle error appropriately
            }
        }

        [HttpGet]
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

        [HttpPost]
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

            // Add string content
            formData.Add(new StringContent(model.Username ?? string.Empty), "Username");
            formData.Add(new StringContent(model.Email ?? string.Empty), "Email");
            formData.Add(new StringContent(model.Bio ?? string.Empty), "Bio");
            formData.Add(new StringContent(model.TalentArea ?? string.Empty), "TalentArea");
            formData.Add(new StringContent(model.ProfilePrivacy.ToString()), "ProfilePrivacy");
            
            // Add existing ProfileImageURL if it's not being replaced
            if (!string.IsNullOrEmpty(model.ProfileImageURL) && model.ProfileImageFile == null)
            {
                formData.Add(new StringContent(model.ProfileImageURL), "ProfileImageUrl");
            }

            // Add file content if a new file is uploaded
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

                // Update the claims with new profile image URL
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

        [HttpGet]
        public IActionResult VerifyCurrentPassword()
        {
            return View();
        }

        [HttpPost]
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

                // First verify the current password using the login endpoint
                var verifyResponse = await client.PostAsJsonAsync("api/Auth/login", new
                {
                    Email = User.FindFirst(ClaimTypes.Email)?.Value,
                    Password = model.CurrentPassword
                });

                if (verifyResponse.IsSuccessStatusCode)
                {
                    // Store both the verification flag and the verified password in Session
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

        [HttpGet]
        public IActionResult ChangePassword()
        {
            // Check if password verification was completed
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("PasswordVerified")))
            {
                return RedirectToAction("VerifyCurrentPassword");
            }

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            // Check if password verification was completed
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

                // Get the verified current password from Session
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
                    // Clear the verification flags from Session
                    HttpContext.Session.Remove("PasswordVerified");
                    HttpContext.Session.Remove("VerifiedCurrentPassword");
                    return View(model); // Show modal on success
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

        [HttpPost]
        public async Task<IActionResult> SendVerificationOTP()
        {
            try
            {
                var token = User.FindFirst("JWT")?.Value;
                if (string.IsNullOrEmpty(token))
                {
                    _logger.LogWarning("JWT token not found in claims");
                    return Json(new { success = false, message = "Not authenticated" });
                }

                var client = _httpClientFactory.CreateClient("API");
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                _logger.LogInformation("Sending verification OTP request to API");
                var response = await client.PostAsync("api/auth/send-verification-otp", null);
                
                var responseContent = await response.Content.ReadAsStringAsync();
                _logger.LogInformation($"API Response: {responseContent}");

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonConvert.DeserializeObject<dynamic>(responseContent);
                    TempData["ResendOTP"] = true;
                    return Json(new { success = true, message = result.message.ToString() });
                }
                else
                {
                    var error = JsonConvert.DeserializeObject<dynamic>(responseContent);
                    _logger.LogError($"Failed to send verification OTP. Status: {response.StatusCode}, Error: {error?.message}");
                    return Json(new { success = false, message = error?.message?.ToString() ?? "Failed to send verification code" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in SendVerificationOTP");
                return Json(new { success = false, message = "An error occurred while sending the verification code" });
            }
        }

        [HttpGet]
        public IActionResult VerifyEmail()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> VerifyEmail([FromBody] VerifyEmailViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "Invalid verification code" });
            }

            try
            {
                var token = User.FindFirst("JWT")?.Value;
                if (string.IsNullOrEmpty(token))
                {
                    return Json(new { success = false, message = "Not authenticated" });
                }

                var client = _httpClientFactory.CreateClient("API");
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var response = await client.PostAsJsonAsync("api/auth/verify-email", new { OTP = model.OTP });
                var responseContent = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<dynamic>(responseContent);

                if (response.IsSuccessStatusCode)
                {
                    // Update the user's claims
                    var claims = User.Claims.ToList();
                    var tokenClaim = claims.FirstOrDefault(c => c.Type == "JWT");
                    if (tokenClaim != null)
                    {
                        claims.Remove(tokenClaim);
                    }
                    claims.Add(new Claim("JWT", result.token.ToString()));
                    claims.Add(new Claim("Verified", "true"));

                    // Sign in the user with updated claims
                    var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    var principal = new ClaimsPrincipal(identity);
                    await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

                    return Json(new { success = true, message = "Email verified successfully" });
                }
                else
                {
                    return Json(new { success = false, message = result?.message?.ToString() ?? "Failed to verify email" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in VerifyEmail");
                return Json(new { success = false, message = "An error occurred while verifying your email" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> ResendVerificationOTP()
        {
            var token = User.FindFirst("JWT")?.Value;
            if (string.IsNullOrEmpty(token))
                return Unauthorized();

            var client = _httpClientFactory.CreateClient("API");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await client.PostAsync("api/auth/resend-verification-otp", null);
            if (!response.IsSuccessStatusCode)
            {
                return Json(new { success = false, message = "Failed to resend verification code" });
            }

            return Json(new { success = true });
        }
    }
}
