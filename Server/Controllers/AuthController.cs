using Microsoft.AspNetCore.Mvc;
using Server.DTOs;
using Server.Models;
using Server.Data;
using Server.Interfaces;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using BCrypt.Net; // Need to add this package
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.Cookies;
using Server.DTOs; // Re-adding to ensure it's present and correct

namespace Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly DatabaseContext _context; // Keep context for other potential actions
    private readonly IAuthRepository _authRepository;
    private readonly IConfiguration _configuration;

    public AuthController(DatabaseContext context, IAuthRepository authRepository, IConfiguration configuration)
    {
        _context = context;
        _authRepository = authRepository;
        _configuration = configuration;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterDTO registerDTO)
    {
        // Check if user with the same email or username already exists using the repository
        if (await _authRepository.UserExists(registerDTO.Email, registerDTO.Username))
        {
            return BadRequest("User with this email or username already exists.");
        }

        // Find the default role (e.g., "User")
        // You might need to ensure a default role exists in your database
        var defaultRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "User");
        if (defaultRole == null)
        {
             // Handle the case where the default role doesn't exist
             return StatusCode(500, "Default role not found. Please configure roles.");
        }

        // Create new user object (password hashing is done in the repository)
        var newUser = new User
        {
            Username = registerDTO.Username,
            Email = registerDTO.Email,
            Bio = "", // Or a default empty string
            ProfileImageURL = "", // Or a default image URL
            RoleID = defaultRole.RoleID,
            TalentArea = registerDTO.TalentArea,
            CreatedAt = DateTime.UtcNow
        };

        // Register the user using the repository
        var registeredUser = await _authRepository.Register(newUser, registerDTO.Password);

        // Return a success response (you might want to return a simplified UserDTO)
        return Ok(new { Message = "User registered successfully!" });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDTO loginDTO)
    {
        // Authenticate the user using the repository
        var user = await _authRepository.Login(loginDTO.Email, loginDTO.Password);

        if (user == null)
        {
            return Unauthorized("Invalid credentials.");
        }

        var token = GenerateToken(user);

        return Ok(new { Token = token });
    }

    [HttpGet("google-challenge")]
    [AllowAnonymous] // Allow anonymous access to initiate the challenge
    public IActionResult GoogleChallenge()
    {
        var properties = new AuthenticationProperties { RedirectUri = Url.Action(nameof(GoogleLoginCallback)) };
        return Challenge(properties, GoogleDefaults.AuthenticationScheme);
    }


    [HttpGet("signin-google")] // Or the configured callback path
    [AllowAnonymous]
    public async Task<IActionResult> GoogleLoginCallback()
    {
        var authenticateResult = await HttpContext.AuthenticateAsync(GoogleDefaults.AuthenticationScheme);

        if (!authenticateResult.Succeeded)
        {
            return BadRequest("Google authentication failed.");
        }

        var googleUser = authenticateResult.Principal;
        var googleIdClaim = googleUser.FindFirst(ClaimTypes.NameIdentifier); // Or the appropriate claim type for Google User ID

        if (googleIdClaim == null)
        {
            return BadRequest("Google User ID not found in claims.");
        }

        var googleId = googleIdClaim.Value;

        // Check if user with Google ID exists
        var user = await _authRepository.GetUserByGoogleId(googleId); // Assuming you add this method to IAuthRepository and AuthRepository

        if (user == null)
        {
            // User doesn't exist, create a new one
            var email = googleUser.FindFirst(ClaimTypes.Email)?.Value;
            var username = googleUser.FindFirst(ClaimTypes.Name)?.Value; // Or generate a username

            if (string.IsNullOrEmpty(email))
            {
                 return BadRequest("Google email not found in claims.");
            }

            // Find the default role (e.g., "User")
            var defaultRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "User");
            if (defaultRole == null)
            {
                 return StatusCode(500, "Default role not found. Please configure roles.");
            }

            var newUser = new User
            {
                GoogleId = googleId,
                Username = username ?? email, // Use username if available, otherwise email
                Email = email,
                Bio = "",
                ProfileImageURL = googleUser.FindFirst("picture")?.Value ?? "", // Get profile picture if available
                RoleID = defaultRole.RoleID,
                TalentArea = "", // Or a default value
                CreatedAt = DateTime.UtcNow
            };

            user = await _authRepository.CreateUser(newUser); // Assuming you add this method to IAuthRepository and AuthRepository
        }

        // Generate JWT for the user
        var token = GenerateToken(user);

        // Create claims for the user
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.UserID.ToString()), // Use UserID as the NameIdentifier
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role.Name)
        };

        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme); // Use Cookie scheme for identity
        var userPrincipal = new ClaimsPrincipal(claimsIdentity);

        // Sign in the user using the default sign-in scheme (GoogleAuthTemp cookie)
        await HttpContext.SignInAsync(userPrincipal);


        return Ok(new { Token = token });
    }

    [HttpGet("profile")]
    [Authorize] // Requires authentication
    public async Task<IActionResult> GetUserProfile()
    {
        // Get the authenticated user's ID from the claims
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
        {
            return Unauthorized("User ID not found in claims.");
        }

        // Fetch the user from the database using the repository
        var user = await _authRepository.GetUserById(userId); // Assuming you add this method to IAuthRepository and AuthRepository

        if (user == null)
        {
            return NotFound("User not found.");
        }

        // Return a simplified user object or DTO
        var userProfile = new UserDTO // Assuming you have a UserDTO
        {
            UserID = user.UserID,
            Username = user.Username,
            Email = user.Email,
            Bio = user.Bio,
            ProfileImageURL = user.ProfileImageURL,
            TalentArea = user.TalentArea
            // Do not include sensitive information like PasswordHash or GoogleId
        };

        return Ok(userProfile);
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
            new Claim("Username", user.Username.ToString()), // Thêm claim chứa tên người dùng
            new Claim("Email", user.Email),                 // Thêm claim chứa email người dùng
            new Claim(ClaimTypes.Role, user.Role.Name)                    // Thêm claim chứa vai trò người dùng
        };

        // Tạo token JWT với các thông tin như issuer, audience, claims, thời gian hết hạn và thông tin ký
        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],          // Định danh của server phát hành token
            audience: _configuration["Jwt:Audience"],      // Định danh của client nhận token
            claims: claims,                                // Danh sách các claim
            expires: DateTime.Now.AddMinutes(30),          // Thời gian hết hạn của token (30 phút)
            signingCredentials: credentials);             // Thông tin ký token

        // Trả về chuỗi token đã được mã hóa
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
