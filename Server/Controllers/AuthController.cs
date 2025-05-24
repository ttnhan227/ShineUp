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
using Google.Apis.Auth;
using Microsoft.Extensions.Logging;

namespace Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthRepository _authRepository;
    private readonly IConfiguration _configuration;
    private readonly DatabaseContext _context;
    private readonly ILogger<AuthController> _logger;
    private readonly IGoogleAuthService _googleAuthService;
    private readonly IEmailService _emailService; // Add IEmailService

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
            Bio = "",
            ProfileImageURL = "",
            RoleID = defaultRole.RoleID,
            TalentArea = registerDTO.TalentArea,
            CreatedAt = DateTime.UtcNow
        };

        // Register the user
        var registeredUser = await _authRepository.Register(newUser, registerDTO.Password);
        return Ok(new { Message = "User registered successfully!" });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDTO loginDTO)
    {
        // Try to find user by either email or username
        var existingUser = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == loginDTO.Email || u.Username == loginDTO.Email);

        if (existingUser == null)
        {
            return Unauthorized("Invalid email/username: Account not found.");
        }

        var user = await _authRepository.Login(loginDTO.Email, loginDTO.Password);

        if (user == null)
        {
            return Unauthorized("Invalid password: The password you entered is incorrect.");
        }

        var token = GenerateToken(user);

        return Ok(new
        {
            Token = token,
            user.Username,
            user.Email,
            user.ProfileImageURL
        });
    }

    [HttpPost("google-login")]
    public async Task<ActionResult<object>> GoogleLogin(GoogleAuthDTO googleAuth)
    {
        try
        {
            var payload = await _googleAuthService.VerifyGoogleToken(googleAuth.IdToken);
            var user = await _googleAuthService.HandleGoogleUser(payload);
            var token = GenerateToken(user);

            // Make sure we're returning the correct profile image URL
            return Ok(new
            {
                Token = token,
                Username = user.Username,
                Email = user.Email,
                ProfileImageURL = user.ProfileImageURL ?? "https://via.placeholder.com/30/007bff/FFFFFF?text=U"
            });
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

    // Add this new endpoint
    [HttpPost("validate-otp")]
    public async Task<IActionResult> ValidateOTP([FromBody] ValidateOTPDTO model)
    {
        try
        {
            var isValid = await _authRepository.ValidateOTP(model.Email, model.OTP);
            if (isValid)
            {
                return Ok(new { message = "OTP validated successfully" });
            }
            return BadRequest(new { message = "Invalid or expired code" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // Modify existing ResetPassword action to not validate OTP again
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

    private string GenerateToken(User user)
    {
        // Tạo khóa bảo mật từ chuỗi ký tự được lưu trong cấu hình (appsettings.json)
        var securityKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));

        // Tạo thông tin ký (credentials) sử dụng thuật toán HMAC-SHA256
        var credentials = new SigningCredentials(
            securityKey, SecurityAlgorithms.HmacSha256);

        // Tạo danh sách các claim (thông tin người dùng) để đưa vào token
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.UserID.ToString()), // Add UserID as NameIdentifier
            new Claim("Username", user.Username), // Thêm claim chứa tên người dùng
            new Claim("Email", user.Email), // Thêm claim chứa email người dùng
            new Claim(ClaimTypes.Role, user.Role.Name) // Thêm claim chứa vai trò người dùng
        };

        // Tạo token JWT với các thông tin như issuer, audience, claims, thời gian hết hạn và thông tin ký
        var token = new JwtSecurityToken(
            _configuration["Jwt:Issuer"], // Định danh của server phát hành token
            _configuration["Jwt:Audience"], // Định danh của client nhận token
            claims, // Danh sách các claim
            expires: DateTime.Now.AddMinutes(30), // Thời gian hết hạn của token (30 phút)
            signingCredentials: credentials); // Thông tin ký token

        // Trả về chuỗi token đã được mã hóa
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
