using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Server.Data;
using Server.DTOs;
using Server.Interfaces;
using Server.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthRepository _authRepository;
    private readonly IConfiguration _configuration;
    private readonly DatabaseContext _context;
    private readonly IEmailService _emailService; // Add IEmailService
    private readonly IGoogleAuthService _googleAuthService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        DatabaseContext context,
        IAuthRepository authRepository,
        IConfiguration configuration,
        ILogger<AuthController> logger,
        IGoogleAuthService googleAuthService,
        IEmailService emailService) // Inject IEmailService
    {
        _context = context;
        _authRepository = authRepository;
        _configuration = configuration;
        _logger = logger;
        _googleAuthService = googleAuthService;
        _emailService = emailService; // Initialize
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterDTO registerDTO)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        // Check both email and username at once
        var emailExists = await _context.Users.AnyAsync(u => u.Email == registerDTO.Email);
        var usernameExists = await _context.Users.AnyAsync(u => u.Username == registerDTO.Username);

        if (emailExists || usernameExists)
        {
            var errors = new Dictionary<string, string[]>();

            if (emailExists)
            {
                errors.Add("Email", new[] { "This email is already registered" });
            }

            if (usernameExists)
            {
                errors.Add("Username", new[] { "This username is already taken" });
            }

            return BadRequest(errors);
        }

        // Find the default role
        var defaultRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "User");
        if (defaultRole == null)
        {
            ModelState.AddModelError("", "Server configuration error");
            return BadRequest(ModelState);
        }

        // Create new user object
        var newUser = new User
        {
            Username = registerDTO.Username,
            Email = registerDTO.Email,
            FullName = registerDTO.FullName,
            Bio = "",
            ProfileImageURL = "",
            RoleID = defaultRole.RoleID,
            TalentArea = registerDTO.TalentArea,
            CreatedAt = DateTime.UtcNow,
            IsActive = false // Set to false until email is verified
        };

        // Register the user
        var registeredUser = await _authRepository.Register(newUser, registerDTO.Password);

        // Generate and send verification OTP
        var otp = await _authRepository.GenerateOTP(registeredUser.Email);
        await _emailService.SendEmailAsync(
            registeredUser.Email,
            "Email Verification",
            $"<p><strong>Dear {registeredUser.Username},</strong></p>\n<p>Welcome to ShineUp! Please verify your email address using the following One-Time Password (OTP):</p>\n<h2 style='color:#007bff; background: #f0f8ff; display: inline-block; padding: 8px 24px; border-radius: 8px; letter-spacing: 2px;'>{otp}</h2>\n<p style='margin-top:16px;'>This code is valid for 15 minutes. Please verify your email to activate your account.</p>\n<p>Thank you,<br/>ShineUp Support Team</p>");

        return Ok(new
        {
            Message = "Registration successful! Please check your email for verification code.",
            UserId = registeredUser.UserID
        });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDTO loginDTO)
    {
        // The repository's Login method now handles finding the user and verifying the password.
        // It returns the user if credentials are valid (and includes the Role), otherwise null.
        // It no longer checks IsActive internally.
        var user = await _authRepository.Login(loginDTO.Email, loginDTO.Password);

        if (user == null)
        {
            // This means either user not found or password was incorrect.
            return Unauthorized("Invalid email/username or password.");
        }

        // Credentials are valid, now check if the account is active.
        if (!user.IsActive)
        {
            // User exists, password is correct, but email is not verified.
            // Return a specific status code and payload that the client can use
            // to redirect to the email verification page.
            return StatusCode(StatusCodes.Status403Forbidden, new
            {
                Message = "Your account is not active. Please verify your email first.",
                RequiresVerification = true,
                user.Email, // Send email back to help client redirect to OTP page for this email
                UserId = user.UserID // Add UserId for the client to use
            });
        }

        // User is active and credentials are valid. Proceed to login.
        var token = GenerateToken(user); // GenerateToken needs user.Role, which _authRepository.Login includes.

        return Ok(new
        {
            Token = token,
            User = new
            {
                user.UserID,
                user.Username,
                user.Email,
                user.ProfileImageURL,
                Role = user.Role.Name,
                user.RoleID,
                user.Verified
            }
        });
    }

    [HttpPost("google-login")]
    public async Task<ActionResult<object>> GoogleLogin(GoogleAuthDTO googleAuth)
    {
        try
        {
            var payload = await _googleAuthService.VerifyGoogleToken(googleAuth.IdToken);
            var user = await _googleAuthService.HandleGoogleUser(payload);

            // Update LastLoginTime for Google logins
            var loginTime = DateTime.UtcNow;

            try
            {
                // Use direct SQL to update the LastLoginTime
                await _context.Database.ExecuteSqlInterpolatedAsync(
                    $"UPDATE \"Users\" SET \"LastLoginTime\" = {loginTime} WHERE \"UserID\" = {user.UserID}");

                // Update the user object in memory
                user.LastLoginTime = loginTime;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"[Google Login] Error saving LastLoginTime for user {user.UserID}");
                throw;
            }

            // For Google users without a password, they need to complete their profile
            var needsPassword = string.IsNullOrEmpty(user.PasswordHash);
            var token = needsPassword ? string.Empty : GenerateToken(user);

            var response = new
            {
                Token = token,
                user.Username,
                user.Email,
                ProfileImageURL = user.ProfileImageURL ?? "https://via.placeholder.com/30/007bff/FFFFFF?text=U",
                user.Verified,
                needsPassword,
                userId = user.UserID
            };

            // If user needs to set a password, include their name info
            if (needsPassword)
            {
                return Ok(new
                {
                    response.Token,
                    response.Username,
                    response.Email,
                    response.ProfileImageURL,
                    response.Verified,
                    response.needsPassword,
                    response.userId,
                    fullName = user.FullName ?? ""
                });
            }

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Google login error: {ex}");
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDTO model)
    {
        try
        {
            // Check if email exists
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == model.Email);
            if (user == null)
            {
                return BadRequest(new { message = "This email doesn't exist" });
            }

            var otp = await _authRepository.GenerateOTP(model.Email);
            await _emailService.SendEmailAsync(
                model.Email,
                "Password Reset Request",
                $"<p><strong>Dear {user.Username},</strong></p>\n<p>We received a request to reset your password. Please use the following One-Time Password (OTP) to proceed:</p>\n<h2 style='color:#007bff; background: #f0f8ff; display: inline-block; padding: 8px 24px; border-radius: 8px; letter-spacing: 2px;'>{otp}</h2>\n<p style='margin-top:16px;'>This code is valid for 15 minutes. If you did not request a password reset, please ignore this email.</p>\n<p>Thank you,<br/>ShineUp Support Team</p>");

            return Ok(new { message = "OTP has been sent to your email" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("CompleteProfile")]
    public IActionResult CompleteProfile(int userId, string email, string fullName)
    {
        _logger.LogInformation($"Serving CompleteProfile page for user ID: {userId}");

        // Return the Razor view from the Client project
        // The view is served by the Client project, so we just need to redirect to it
        // The Client project should handle the routing to the correct view
        return Redirect($"~/auth/completeprofile?userId={userId}&email={email}&fullName={fullName}");
    }

    [HttpPost("CompleteGoogleProfile")]
    [AllowAnonymous]
    public async Task<IActionResult> CompleteGoogleProfile([FromBody] CompleteProfileDTO model)
    {
        try
        {
            _logger.LogInformation($"Starting profile completion for user ID: {model?.UserId}");

            // Validate the model
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                _logger.LogWarning($"Model validation failed: {string.Join(", ", errors)}");
                return BadRequest(new
                {
                    success = false,
                    message = "Validation failed",
                    errors
                });
            }

            // Find the user
            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.UserID == model.UserId);

            if (user == null)
            {
                _logger.LogWarning($"User not found with ID: {model.UserId}");
                return NotFound(new
                {
                    success = false,
                    message = "User not found"
                });
            }

            // Check if the username is already taken by another user
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Username.ToLower() == model.Username.ToLower() && u.UserID != model.UserId);

            if (existingUser != null)
            {
                _logger.LogWarning($"Username {model.Username} is already taken");
                return BadRequest(new
                {
                    success = false,
                    message = "Username is already taken"
                });
            }


            _logger.LogInformation($"Updating user profile for user ID: {user.UserID}");

            // Update user information
            user.Username = model.Username.Trim();
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password);
            user.FullName = model.FullName.Trim();

            // Update user profile information
            user.TalentArea = model.TalentArea?.Trim();


            // Save changes
            await _context.SaveChangesAsync();
            _logger.LogInformation($"Successfully updated profile for user ID: {user.UserID}");

            // Generate JWT token
            var token = GenerateToken(user);

            // Get the user's role name
            var roleName = user.Role?.Name ?? "User";

            _logger.LogInformation($"Profile completion successful for user ID: {user.UserID}");

            return Ok(new
            {
                success = true,
                token,
                username = user.Username,
                email = user.Email,
                profileImageURL = user.ProfileImageURL ?? "https://via.placeholder.com/30/007bff/FFFFFF?text=U",
                role = roleName,
                fullName = user.FullName,
                redirectUrl = "/"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error completing Google profile for user ID: {model?.UserId}");
            return StatusCode(500, new
            {
                success = false,
                message = "An error occurred while completing your profile. Please try again."
            });
        }
    }

    [HttpPost("validate-otp")]
    public async Task<IActionResult> ValidateOTP([FromBody] ValidateOTPDTO model)
    {
        try
        {
            var isValid = await _authRepository.ValidateOTP(model.Email, model.OTP);
            if (isValid)
            {
                // Get the user and activate their account
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == model.Email);
                if (user != null)
                {
                    user.IsActive = true;
                    user.Verified = true;
                    await _context.SaveChangesAsync();
                }

                return Ok(new { message = "Email verified successfully" });
            }

            return BadRequest(new { message = "Invalid or expired code" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // Removed duplicate CompleteGoogleProfile endpoint - using the more robust implementation above

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDTO model)
    {
        try
        {
            if (await _authRepository.ResetPassword(model.Email, model.NewPassword))
            {
                return Ok(new { message = "Password has been reset successfully" });
            }

            return BadRequest(new { message = "Password reset failed" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [Authorize]
    [HttpPost("send-verification-otp")]
    public async Task<IActionResult> SendVerificationOTP()
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
            {
                _logger.LogWarning(
                    $"Invalid user ID claim. Claims: {string.Join(", ", User.Claims.Select(c => $"{c.Type}: {c.Value}"))}");
                return BadRequest(new { success = false, message = "Invalid user ID" });
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserID == userId);
            if (user == null)
            {
                _logger.LogWarning($"User not found for ID: {userId}");
                return NotFound(new { success = false, message = "User not found" });
            }

            if (user.Verified)
            {
                _logger.LogInformation($"User {userId} is already verified");
                return BadRequest(new { success = false, message = "Email is already verified" });
            }

            // Generate OTP via repository (handles hashing and saving)
            var otp = await _authRepository.GenerateOTP(user.Email);

            // Send OTP email
            var emailBody = $@"
                <h2>Email Verification</h2>
                <p>Dear {user.Username},</p>
                <p>Please use the following code to verify your email address:</p>
                <h1 style='color: #5e72e4; background: #f0f8ff; display: inline-block; padding: 8px 24px; border-radius: 8px; letter-spacing: 2px;'>{otp}</h1>
                <p>This code will expire in 15 minutes.</p>
                <p>If you did not request this verification, please ignore this email.</p>
                <p>Thank you,<br/>ShineUp Support Team</p>";

            await _emailService.SendEmailAsync(user.Email, "Email Verification", emailBody);

            _logger.LogInformation($"Verification OTP sent to user {userId}");
            return Ok(new { success = true, message = "Verification code sent successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending verification OTP");
            return StatusCode(500, new { success = false, message = "Internal server error" });
        }
    }

    [HttpPost("verify-email")]
    public async Task<IActionResult> VerifyEmail([FromBody] VerifyEmailDTO model)
    {
        try
        {
            var isOtpValid = await _authRepository.VerifyEmail(model.Email, model.OTP);

            if (!isOtpValid)
            {
                // _authRepository.VerifyEmail logs details, so a generic message here is fine.
                // It handles cases like OTP not found, expired, or invalid.
                return BadRequest(new { message = "Invalid or expired verification code. Please request a new one." });
            }

            // OTP is valid, _authRepository.VerifyEmail has marked OTP as used and user.Verified = true.
            // Now, fetch the user to activate and generate a token.
            var user = await _context.Users
                .Include(u => u.Role) // Ensure Role is loaded for token generation
                .FirstOrDefaultAsync(u => u.Email == model.Email);

            if (user == null)
            {
                // This case should ideally not happen if VerifyEmail succeeded, but as a safeguard:
                _logger.LogError($"User not found for email {model.Email} after successful OTP verification.");
                return StatusCode(500, new { message = "Internal server error during verification." });
            }

            // Activate the account if it's not already (VerifyEmail sets Verified, not IsActive)
            if (!user.IsActive)
            {
                user.IsActive = true;
                await _context.SaveChangesAsync();
            }

            // Generate new token with updated claims
            var token = GenerateToken(user);

            return Ok(new
            {
                message = "Email verified successfully",
                token,
                user = new
                {
                    username = user.Username,
                    email = user.Email,
                    verified = user.Verified, // Should be true now
                    profileImageURL = user.ProfileImageURL
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error verifying email: {ex.Message}");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [Authorize]
    [HttpPost("resend-verification-otp")]
    public async Task<IActionResult> ResendVerificationOTP()
    {
        try
        {
            var userId = int.Parse(User.FindFirst("UserID")?.Value);
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserID == userId);
            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            if (user.Verified)
            {
                return BadRequest(new { message = "Email is already verified" });
            }

            var otp = await _authRepository.GenerateOTP(user.Email);
            // _authRepository.GenerateOTP already saves the hashed OTP.

            // Send OTP email
            var emailBody = $"Your verification code is: {otp}";
            await _emailService.SendEmailAsync(user.Email, "Email Verification", emailBody);

            return Ok(new { message = "Verification code resent successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error resending verification OTP: {ex.Message}");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    private string GenerateToken(User user)
    {
        _logger.LogInformation(
            $"[AuthController] Generating token for user ID: {user.UserID}, Email: {user.Email}, Username: {user.Username}");

        var securityKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));

        var credentials = new SigningCredentials(
            securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.UserID.ToString()),
            new Claim("Username", user.Username),
            new Claim("Email", user.Email),
            new Claim(ClaimTypes.Role, user.Role?.Name ?? "User"),
            new Claim("RoleID", user.RoleID.ToString())
        };

        _logger.LogInformation(
            $"[AuthController] Token claims: {string.Join(", ", claims.Select(c => $"{c.Type}: {c.Value}"))}");

        var token = new JwtSecurityToken(
            _configuration["Jwt:Issuer"],
            _configuration["Jwt:Audience"],
            claims,
            expires: DateTime.Now.AddDays(7), // Changed from 30 minutes to 7 days
            signingCredentials: credentials);


        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);
        _logger.LogInformation($"[AuthController] Token generated successfully for user ID: {user.UserID}");

        return tokenString;
    }
}