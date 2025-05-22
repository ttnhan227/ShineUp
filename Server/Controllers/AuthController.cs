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
        var user = await _authRepository.Login(loginDTO.Email, loginDTO.Password);

        if (user == null)
        {
            return Unauthorized("Invalid email or password.");
        }

        var token = GenerateToken(user);
        
        // Add this line to log the token
        Console.WriteLine($"JWT Token generated for {user.Email}: {token}");

        return Ok(new { 
            Token = token,
            Username = user.Username,
            Email = user.Email
        });
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
