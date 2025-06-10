using Client.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;

namespace Client.Controllers;

[Route("[controller]")]
public class AuthController : Controller
{
    private readonly IConfiguration _configuration;
    private readonly HttpClient _httpClient;
    private readonly ILogger<AuthController> _logger;

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
            var emailPattern = @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$";
            if (!Regex.IsMatch(model.Email, emailPattern))
            {
                ModelState.AddModelError("Email", "Please enter a valid email address (e.g., example@gmail.com)");
                return View(model);
            }

            var json = JsonConvert.SerializeObject(model);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("api/Auth/register", content);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var result = JsonConvert.DeserializeObject<dynamic>(responseContent);
                TempData["UserId"] = result.UserId.ToString();
                TempData["Email"] = model.Email;
                return RedirectToAction("VerifyEmail");
            }

            try
            {
                var errors = JsonConvert.DeserializeObject<Dictionary<string, string[]>>(responseContent);
                if (errors != null)
                {
                    foreach (var error in errors)
                    foreach (var message in error.Value)
                        ModelState.AddModelError(error.Key, message);
                }
            }
            catch
            {
                ModelState.AddModelError("", responseContent);
            }
        }

        return View(model);
    }

    [HttpGet("verify-email")]
    public IActionResult VerifyEmail()
    {
        var email = TempData["Email"]?.ToString();
        var userId = TempData["UserId"]?.ToString();

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(userId))
        {
            return RedirectToAction("Register");
        }

        TempData.Keep("Email");
        TempData.Keep("UserId");

        return View(new VerifyEmailViewModel { Email = email });
    }

    [HttpPost("verify-email")]
    public async Task<IActionResult> VerifyEmail(VerifyEmailViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            _logger.LogInformation($"Attempting to verify email: {model.Email}");

            var verifyResponse = await _httpClient.PostAsJsonAsync("api/Auth/verify-email", new
            {
                model.Email, model.OTP
            });

            var responseContent = await verifyResponse.Content.ReadAsStringAsync();
            _logger.LogInformation($"Verification response: {responseContent}");

            if (verifyResponse.IsSuccessStatusCode)
            {
                var result = JsonConvert.DeserializeObject<dynamic>(responseContent);

                if (result == null)
                {
                    _logger.LogError("Failed to deserialize verification response");
                    ModelState.AddModelError("", "Invalid response from server");
                    return View(model);
                }

                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, result.user?.username?.ToString() ?? model.Email),
                    new Claim(ClaimTypes.Email, result.user?.email?.ToString() ?? model.Email),
                    new Claim(ClaimTypes.NameIdentifier, result.user?.UserID?.ToString() ?? "0"),
                    new Claim(ClaimTypes.Role, result.user?.Role?.ToString() ?? "User"),
                    new Claim("RoleID", result.user?.RoleID?.ToString() ?? "1"),
                    new Claim("JWT", result.token?.ToString() ?? string.Empty),
                    new Claim("ProfileImageURL",
                        result.user?.profileImageURL?.ToString() ??
                        "https://via.placeholder.com/30/007bff/FFFFFF?text=U"),
                    new("Verified", "true")
                };

                var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var principal = new ClaimsPrincipal(identity);

                Response.Cookies.Append("auth_token", result.token?.ToString() ?? string.Empty, new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Lax,
                    Expires = DateTime.UtcNow.AddHours(24)
                });

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

            var error = JsonConvert.DeserializeObject<Dictionary<string, string>>(responseContent);
            ModelState.AddModelError("OTP", error?["message"] ?? "Invalid or expired verification code");
            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during email verification");
            ModelState.AddModelError("", "An error occurred during verification");
            return View(model);
        }
    }

    [HttpPost("resend-verification")]
    public async Task<IActionResult> ResendVerification([FromBody] ForgotPasswordViewModel model)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/Auth/send-verification-otp", new { model.Email });
            var responseContent = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<Dictionary<string, string>>(responseContent);

            if (response.IsSuccessStatusCode)
            {
                return Json(new { success = true, message = "Verification code has been resent" });
            }

            return Json(new { success = false, message = result?["message"] ?? "Failed to resend verification code" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resending verification code");
            return Json(new { success = false, message = "An error occurred" });
        }
    }

    [HttpGet("login")]
    public IActionResult Login(string returnUrl = null)
    {
        ViewBag.ReturnUrl = returnUrl;
        return View();
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginViewModel model, string returnUrl = null)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View(model);
        }

        try
        {
            var client = _httpClient;
            var response = await client.PostAsJsonAsync("api/auth/login", new
            {
                model.Email, model.Password
            });

            var content = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var result = JsonConvert.DeserializeObject<dynamic>(content);

                if (result == null)
                {
                    ModelState.AddModelError("", "Invalid response from server.");
                    ViewBag.ReturnUrl = returnUrl;
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
                    new Claim("ProfileImageURL",
                        result.User.ProfileImageURL?.ToString() ??
                        "https://via.placeholder.com/30/007bff/FFFFFF?text=U"),
                    new Claim("Verified", result.User.Verified.ToString())
                };

                var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var principal = new ClaimsPrincipal(identity);

                Response.Cookies.Append("auth_token", result.Token.ToString(), new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Lax,
                    Expires = DateTime.UtcNow.AddDays(7)
                });

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    principal,
                    new AuthenticationProperties
                    {
                        IsPersistent = true,
                        ExpiresUtc = DateTime.UtcNow.AddDays(7)
                    });

                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    return Redirect(returnUrl);
                }

                return RedirectToAction("Index", "Home");
            }

            if (response.StatusCode == HttpStatusCode.Forbidden)
            {
                try
                {
                    var errorDetails = JsonConvert.DeserializeObject<VerificationRequiredResponse>(content);
                    if (errorDetails != null && errorDetails.RequiresVerification)
                    {
                        TempData["Email"] = errorDetails.Email;
                        TempData["UserId"] = errorDetails.UserId.ToString();
                        TempData["VerificationMessage"] = errorDetails.Message;
                        return RedirectToAction("VerifyEmail");
                    }
                }
                catch (JsonException jsonEx)
                {
                    _logger.LogError(jsonEx, "Failed to deserialize 403 Forbidden response during login.");
                }
            }

            var errorMessage = content;
            try
            {
                var error = JsonConvert.DeserializeObject<dynamic>(content);
                if (error != null && error.message != null)
                {
                    errorMessage = error.message.ToString().Trim('"');
                }
                else
                {
                    errorMessage = content.Trim('"');
                }
            }
            catch
            {
                errorMessage = content.Trim('"');
            }

            ModelState.AddModelError("", errorMessage);
            ViewBag.ReturnUrl = returnUrl;
            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login");
            ModelState.AddModelError("", "An error occurred during login.");
            ViewBag.ReturnUrl = returnUrl;
            return View(model);
        }
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        // Clear the authentication cookie
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        // Clear the session
        HttpContext.Session.Clear();

        // Remove the JWT token cookie
        Response.Cookies.Delete("auth_token", new CookieOptions
        {
            HttpOnly = true,
            Secure = true, // Ensure this matches how the cookie was set (should be true in production)
            SameSite = SameSiteMode.Lax,
            Path = "/" // Ensure we're deleting the cookie from the correct path
        });

        // Clear any lingering authentication state
        HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity());

        // Add cache control headers to prevent caching of authenticated pages
        Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
        Response.Headers["Pragma"] = "no-cache";
        Response.Headers["Expires"] = "0";

        return RedirectToAction("Login", "Auth");
    }

    private async Task SignInUserAsync(CompleteProfileResponse response)
    {
        try
        {
            var token = response.Token;
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, response.UserId.ToString()),
                new(ClaimTypes.Name, response.Username),
                new(ClaimTypes.Email, response.Email),
                new(ClaimTypes.Role, response.Role),
                new("Token", token)
            };

            var claimsIdentity = new ClaimsIdentity(
                claims, CookieAuthenticationDefaults.AuthenticationScheme);

            var authProperties = new AuthenticationProperties
            {
                AllowRefresh = true,
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7)
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);

            HttpContext.Session.SetString("UserId", response.UserId.ToString());
            HttpContext.Session.SetString("Username", response.Username);
            HttpContext.Session.SetString("Email", response.Email);
            HttpContext.Session.SetString("ProfileImageURL", response.ProfileImageURL ?? string.Empty);
            HttpContext.Session.SetString("Role", response.Role);
            HttpContext.Session.SetString("Token", token);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error signing in user: {ex.Message}");
            throw;
        }
    }

    [HttpPost("google-auth")]
    public async Task<IActionResult> GoogleAuth([FromBody] GoogleAuthViewModel model)
    {
        try
        {
            _logger.LogInformation("Starting Google auth...");
            var apiUrl = $"{_configuration["ApiBaseUrl"]}/api/Auth/google-login";

            var response = await _httpClient.PostAsJsonAsync(apiUrl, new { model.IdToken });
            var content = await response.Content.ReadAsStringAsync();
            _logger.LogInformation($"API Response: {content}");

            if (!response.IsSuccessStatusCode)
            {
                return Json(new { success = false, message = content });
            }

            var responseObj = JsonConvert.DeserializeObject<dynamic>(content);
            if (responseObj?.needsPassword != null && (bool)responseObj.needsPassword)
            {
                return Json(new
                {
                    success = true,
                    needsPassword = true,
                    userId = (int)responseObj.userId,
                    email = (string)responseObj.email,
                    username = (string)(responseObj.username ?? ""),
                    fullName = (string)(responseObj.fullName ?? "")
                });
            }

            var result = JsonConvert.DeserializeObject<LoginResponseDTO>(content);
            if (result?.Token == null)
            {
                return Json(new { success = false, message = "No token received" });
            }

            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadToken(result.Token) as JwtSecurityToken;
            var claims = new List<Claim>
            {
                new(ClaimTypes.Name, result.Username),
                new(ClaimTypes.Email, result.Email),
                new("JWT", result.Token),
                new("ProfileImageURL", result.ProfileImageURL ?? "https://via.placeholder.com/30/007bff/FFFFFF?text=U"),
                new("IsGoogleAccount", result.IsGoogleAccount.ToString())
            };

            if (jwtToken != null)
            {
                var userIdClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
                var roleClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role);
                var roleIdClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "RoleID");

                if (userIdClaim != null)
                {
                    claims.Add(new Claim(ClaimTypes.NameIdentifier, userIdClaim.Value));
                }

                if (roleClaim != null)
                {
                    claims.Add(new Claim(ClaimTypes.Role, roleClaim.Value));
                }

                if (roleIdClaim != null)
                {
                    claims.Add(new Claim("RoleID", roleIdClaim.Value));
                }
            }

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

            return Json(new
            {
                success = true,
                token = result.Token,
                username = result.Username,
                email = result.Email,
                profileImageURL = result.ProfileImageURL,
                isGoogleAccount = result.IsGoogleAccount,
                redirectUrl = "/"
            });
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

        TempData.Keep("Email");
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
                return View(model);
            }

            ModelState.AddModelError("NewPassword",
                result?["message"] ?? "Failed to reset password. Please try again.");
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

    [HttpGet("CompleteProfile")]
    [HttpGet("complete-profile")]
    public IActionResult CompleteProfile(int userId, string email, string fullName = "", string username = "")
    {
        var model = new CompleteProfileViewModel
        {
            UserId = userId,
            Email = email,
            Username = username,
            FullName = fullName
        };
        return View(model);
    }

    [HttpPost("CompleteProfile")]
    [HttpPost("complete-profile")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CompleteProfile(CompleteProfileViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            _logger.LogInformation($"Starting profile completion for user {model.UserId}");

            var requestData = new
            {
                model.UserId,
                model.Username,
                model.FullName,
                model.TalentArea,
                model.Password,
                model.ConfirmPassword
            };

            _logger.LogInformation(
                $"Sending request to API: {JsonConvert.SerializeObject(requestData, Formatting.Indented)}");

            var response = await _httpClient.PostAsJsonAsync("api/Auth/CompleteGoogleProfile", requestData);
            var responseContent = await response.Content.ReadAsStringAsync();
            _logger.LogInformation($"API Response: {response.StatusCode} - {responseContent}");

            if (response.IsSuccessStatusCode)
            {
                var result = JsonConvert.DeserializeObject<CompleteProfileResponse>(responseContent);
                if (result == null)
                {
                    throw new JsonException("Failed to deserialize the response");
                }

                _logger.LogInformation(
                    $"Successfully completed profile for user {result.Username} (ID: {result.UserId})");

                // Set auth token in cookie first
                Response.Cookies.Append("auth_token", result.Token, new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Lax,
                    Expires = DateTime.UtcNow.AddDays(7)
                });

                // Create claims for the user - matching the normal login flow
                var claims = new List<Claim>
                {
                    new(ClaimTypes.Name, result.Username ?? string.Empty),
                    new(ClaimTypes.Email, result.Email ?? string.Empty),
                    new(ClaimTypes.NameIdentifier, result.UserId.ToString()),
                    new(ClaimTypes.Role, result.Role ?? "User"),
                    new("JWT", result.Token),
                    new("ProfileImageURL",
                        result.ProfileImageURL ?? "https://via.placeholder.com/30/007bff/FFFFFF?text=U"),
                    new("IsGoogleAccount", "true")
                };

                // Add RoleID claim
                claims.Add(new Claim("RoleID", result.RoleId.ToString()));

                var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var principal = new ClaimsPrincipal(identity);

                // Sign in the user with the same settings as normal login
                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    principal,
                    new AuthenticationProperties
                    {
                        IsPersistent = true,
                        ExpiresUtc = DateTime.UtcNow.AddDays(7)
                    });

                // Set session variables
                HttpContext.Session.SetString("UserId", result.UserId.ToString());
                HttpContext.Session.SetString("Username", result.Username);
                HttpContext.Session.SetString("Email", result.Email);
                HttpContext.Session.SetString("ProfileImageURL",
                    result.ProfileImageURL ?? "https://via.placeholder.com/30/007bff/FFFFFF?text=U");

                TempData["SuccessMessage"] = "Your profile has been updated successfully!";

                return RedirectToAction("Index", "Home");
            }

            var errorResponse = JsonConvert.DeserializeObject<Dictionary<string, object>>(responseContent);
            if (errorResponse?.ContainsKey("message") == true)
            {
                ModelState.AddModelError("", errorResponse["message"].ToString());
            }
            else if (errorResponse?.ContainsKey("errors") == true)
            {
                var errors =
                    JsonConvert.DeserializeObject<Dictionary<string, string[]>>(errorResponse["errors"].ToString());
                foreach (var error in errors.Values.SelectMany(e => e)) ModelState.AddModelError("", error);
            }
            else
            {
                ModelState.AddModelError("", "An error occurred while completing your profile.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error completing profile");
            ModelState.AddModelError("", "An error occurred while completing your profile. Please try again.");
        }

        return View(model);
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

public class CompleteProfileResponse
{
    public string Token { get; set; }
    public int UserId { get; set; }
    public string Username { get; set; }
    public string Email { get; set; }
    public string ProfileImageURL { get; set; }
    public string Role { get; set; }
    public int RoleId { get; set; }
}

public class VerificationRequiredResponse
{
    public string Message { get; set; }
    public bool RequiresVerification { get; set; }
    public string Email { get; set; }
    public int UserId { get; set; }
}