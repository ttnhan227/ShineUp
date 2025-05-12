using Microsoft.AspNetCore.Mvc;
using Server.DTOs;
using Server.Models;
using Server.Data;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using BCrypt.Net; // Need to add this package

namespace Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly DatabaseContext _context;

    public AuthController(DatabaseContext context)
    {
        _context = context;
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
}
