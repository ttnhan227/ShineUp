using Microsoft.AspNetCore.Mvc;
using Server.DTOs;
using Server.Models;
using Server.Data;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using BCrypt.Net; // Need to add this package
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly DatabaseContext _context;
    private readonly IConfiguration _configuration;

    public AuthController(DatabaseContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterDTO registerDTO)
    {
        // Check if user with the same email or username already exists
        if (await _context.Users.AnyAsync(u => u.Email == registerDTO.Email || u.Username == registerDTO.Username))
        {
            return BadRequest("User with this email or username already exists.");
        }

        // Hash the password
        string passwordHash = BCrypt.Net.BCrypt.HashPassword(registerDTO.Password);

        // Find the default role (e.g., "User")
        // You might need to ensure a default role exists in your database
        var defaultRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "User");
        if (defaultRole == null)
        {
             // Handle the case where the default role doesn't exist
             return StatusCode(500, "Default role not found. Please configure roles.");
        }


        // Create new user
        var newUser = new User
        {
            Username = registerDTO.Username,
            Email = registerDTO.Email,
            PasswordHash = passwordHash,
            Bio = "", // Or a default empty string
            ProfileImageURL = "", // Or a default image URL
            RoleID = defaultRole.RoleID,
            TalentArea = registerDTO.TalentArea,
            CreatedAt = DateTime.UtcNow
        };

        _context.Users.Add(newUser);
        await _context.SaveChangesAsync();

        // Return a success response (you might want to return a simplified UserDTO)
        return Ok(new { Message = "User registered successfully!" });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDTO loginDTO)
    {
        var user = await Authenticate(loginDTO);

        if (user == null)
        {
            return Unauthorized("Invalid credentials.");
        }

        var token = GenerateToken(user);

        return Ok(new { Token = token });
    }

    private async Task<User> Authenticate(LoginDTO userLogin)
    {
        // Find the user in the database based on email
        var user = await _context.Users
            .Include(u => u.Role) // Include the Role for claims
            .SingleOrDefaultAsync(x => x.Email.Equals(userLogin.Email));

        // Verify the password using BCrypt
        if (user != null && BCrypt.Net.BCrypt.Verify(userLogin.Password, user.PasswordHash))
        {
            return user;
        }

        return null; // Authentication failed
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
