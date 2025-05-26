using Microsoft.AspNetCore.Mvc;
using Client.Models;
using System.Threading.Tasks;
using System.Net.Http;
using System.Text;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using Microsoft.Extensions.Configuration;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using System.IdentityModel.Tokens.Jwt; // Added for JWT token parsing
using Microsoft.Extensions.Logging; // Added for logging

namespace Client.Controllers
{
    [Route("[controller]")]
    public class AuthController : Controller
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthController> _logger; // Added logger

        public AuthController(HttpClient httpClient, IConfiguration configuration, ILogger<AuthController> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
        }

        [HttpGet("register")]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                var json = JsonConvert.SerializeObject(model);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("api/Auth/register", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    return RedirectToAction("Login");
                }

                try
                {
                    var errors = JsonConvert.DeserializeObject<Dictionary<string, string[]>>(responseContent);
                    if (errors != null)
                    {
                        foreach (var error in errors)
                        {
                            foreach (var message in error.Value)
                            {
                                ModelState.AddModelError(error.Key, message);
                            }
                        }
                    }
                }
                catch
                {
                    ModelState.AddModelError("", responseContent);
                }
            }
            return View(model);
        }

        [HttpGet("login")]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var client = _httpClient;
                var response = await client.PostAsJsonAsync("api/auth/login", new
                {
                    Email = model.Email,
                    Password = model.Password
                });

                var content = await response.Content.ReadAsStringAsync();
                
                if (response.IsSuccessStatusCode)
                {
                    var result = JsonConvert.DeserializeObject<dynamic>(content);
                    
                    if (result == null)
                    {
                        ModelState.AddModelError("", "Invalid response from server.");
                        return View(model);
                    }

                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.Name, result.User.Username.ToString()),
                        new Claim(ClaimTypes.Email, result.User.Email.ToString()),
                        new Claim(ClaimTypes.NameIdentifier, result.User.UserID.ToString()),
                        new Claim(ClaimTypes.Role, result.User.Role.ToString()),
                        new Claim("RoleID", result.User.RoleID.ToString()),
                        new Claim("JWT", result.Token.ToString()),
                        new Claim("ProfileImageURL", result.User.ProfileImageURL?.ToString() ?? "https://via.placeholder.com/30/007bff/FFFFFF?text=U"),
                        new Claim("Verified", result.User.Verified.ToString())
                    };

                    var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    var principal = new ClaimsPrincipal(identity);

                    await HttpContext.SignInAsync(
                        CookieAuthenticationDefaults.AuthenticationScheme,
                        principal,
                        new AuthenticationProperties
                        {
                            IsPersistent = true,
                            ExpiresUtc = DateTime.UtcNow.AddHours(24)
                        });

                    return RedirectToAction("Index", "Home");
                }
                else
                {
                    var errorMessage = content;
                    try
                    {
                        // Try to parse the error message from the response
                        var error = JsonConvert.DeserializeObject<dynamic>(content);
                        if (error != null && error.message != null)
                        {
                            errorMessage = error.message.ToString().Trim('"');
                        }
                        else
                        {
                            // If we can't parse the error message, use the raw content
                            errorMessage = content.Trim('"');
                        }
                    }
                    catch
                    {
                        // If we can't parse the error message, use the raw content
                        errorMessage = content.Trim('"');
                    }

                    // Add the error message to ModelState
                    ModelState.AddModelError("", errorMessage);
                    return View(model);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login");
                ModelState.AddModelError("", "An error occurred during login.");
                return View(model);
            }
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Auth");
        }

        [HttpPost("google-auth")]
        public async Task<IActionResult> GoogleAuth([FromBody] GoogleAuthViewModel model)
        {
            try
            {
                _logger.LogInformation("Starting Google auth...");
                var apiUrl = $"{_configuration["ApiBaseUrl"]}/api/Auth/google-login";
                
                var response = await _httpClient.PostAsJsonAsync(apiUrl, new { IdToken = model.IdToken });
                var content = await response.Content.ReadAsStringAsync();
                _logger.LogInformation($"API Response: {content}");

                if (!response.IsSuccessStatusCode)
                {
                    return Json(new { success = false, message = content });
                }

                var result = JsonConvert.DeserializeObject<LoginResponseDTO>(content);
                if (result?.Token == null)
                {
                    return Json(new { success = false, message = "No token received" });
                }

                var handler = new JwtSecurityTokenHandler();
                var jwtToken = handler.ReadToken(result.Token) as JwtSecurityToken;                var claims = new List<Claim>
                {
                    new(ClaimTypes.Name, result.Username),
                    new(ClaimTypes.Email, result.Email),
                    new("JWT", result.Token),
                    // Ensure profile image URL is properly set
                    new("ProfileImageURL", result.ProfileImageURL ?? "https://via.placeholder.com/30/007bff/FFFFFF?text=U"),
                    new("IsGoogleAccount", result.IsGoogleAccount.ToString())
                };

                // Extract UserID, Role and RoleID from JWT
                if (jwtToken != null)
                {
                    var userIdClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
                    var roleClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role);
                    var roleIdClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "RoleID");

                    if (userIdClaim != null)
                        claims.Add(new Claim(ClaimTypes.NameIdentifier, userIdClaim.Value));
                    if (roleClaim != null)
                        claims.Add(new Claim(ClaimTypes.Role, roleClaim.Value));
                    if (roleIdClaim != null)
                        claims.Add(new Claim("RoleID", roleIdClaim.Value));
                }

                // Log the profile image URL for debugging
                _logger.LogInformation($"Setting profile image URL: {result.ProfileImageURL}");

                var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var principal = new ClaimsPrincipal(identity);

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    principal,
                    new AuthenticationProperties 
                    { 
                        IsPersistent = true,
                        ExpiresUtc = DateTime.UtcNow.AddHours(24)
                    });

                return Json(new { success = true, redirectUrl = "/" });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Google auth error: {ex}");
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet("forgot-password")]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                var response = await _httpClient.PostAsJsonAsync("api/Auth/forgot-password", model);
                var responseContent = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<Dictionary<string, string>>(responseContent);

                if (response.IsSuccessStatusCode)
                {
                    TempData["Email"] = model.Email;
                    return RedirectToAction("ConfirmOTP");
                }

                ModelState.AddModelError("Email", result?["message"] ?? "Failed to send reset code. Please try again.");
            }
            return View(model);
        }

        [HttpGet("confirm-otp")]
        public IActionResult ConfirmOTP()
        {
            var email = TempData["Email"]?.ToString();
            if (string.IsNullOrEmpty(email))
            {
                return RedirectToAction("ForgotPassword");
            }
            TempData.Keep("Email"); // Keep the email for the next request
            return View(new ConfirmOTPViewModel { Email = email });
        }

        [HttpPost("confirm-otp")]
        public async Task<IActionResult> ConfirmOTP(ConfirmOTPViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var response = await _httpClient.PostAsJsonAsync("api/Auth/validate-otp", model);
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var result = JsonConvert.DeserializeObject<Dictionary<string, string>>(responseContent);

                    if (response.IsSuccessStatusCode)
                    {
                        TempData["Email"] = model.Email;
                        TempData["OTP"] = model.OTP;
                        return RedirectToAction("ResetPassword");
                    }

                    ModelState.AddModelError("OTP", result?["message"] ?? "Invalid or expired code. Please try again.");
                }
                catch (Exception ex)
                {
                    _logger.LogError($"OTP validation error: {ex}");
                    ModelState.AddModelError("", "An error occurred. Please try again.");
                }
            }
            return View(model);
        }

        [HttpGet("reset-password")]
        public IActionResult ResetPassword()
        {
            var email = TempData["Email"]?.ToString();
            var otp = TempData["OTP"]?.ToString();
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(otp))
            {
                return RedirectToAction("ForgotPassword");
            }
            return View(new ResetPasswordViewModel { Email = email, OTP = otp });
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                var response = await _httpClient.PostAsJsonAsync("api/Auth/reset-password", model);
                var responseContent = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<Dictionary<string, string>>(responseContent);

                if (response.IsSuccessStatusCode)
                {
                    TempData["PasswordResetSuccess"] = true;
                    return View(model); // This will trigger the modal
                }

                ModelState.AddModelError("NewPassword", result?["message"] ?? "Failed to reset password. Please try again.");
            }
            return View(model);
        }

        [HttpPost("resend-otp")]
        public async Task<IActionResult> ResendOTP([FromBody] ForgotPasswordViewModel model)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("api/Auth/forgot-password", model);
                var responseContent = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<Dictionary<string, string>>(responseContent);

                if (response.IsSuccessStatusCode)
                {
                    TempData["ResendOTP"] = true;
                    return Json(new { success = true, message = "Code has been resent" });
                }

                return Json(new { success = false, message = result?["message"] ?? "Failed to resend code" });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error resending OTP: {ex}");
                return Json(new { success = false, message = "An error occurred" });
            }
        }
    }

    public class LoginResponseDTO
    {
        public string Token { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string? ProfileImageURL { get; set; }
        public bool IsGoogleAccount { get; set; }
    }

    public class GoogleAuthDTO
    {
        public string IdToken { get; set; }
    }
}
