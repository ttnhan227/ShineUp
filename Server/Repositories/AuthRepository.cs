using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Interfaces;
using Server.Models;

namespace Server.Repositories;

public class AuthRepository : IAuthRepository
{
    private readonly DatabaseContext _context;

    public AuthRepository(DatabaseContext context)
    {
        _context = context;
    }

    public async Task<User> Register(User user, string password)
    {
        // Hash the password
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(password);
        user.PasswordHash = passwordHash;

        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        return user;
    }

    public async Task<User> Login(string email, string password)
    {
        var user = await _context.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(x => x.Email.Equals(email));

        if (user == null)
        {
            return null;
        }

        if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
        {
            return null;
        }

        return user;
    }

    public async Task<bool> UserExists(string email, string username)
    {
        if (await _context.Users.AnyAsync(u => u.Email == email || u.Username == username))
        {
            return true;
        }

        return false;
    }

    public async Task<User> CreateUser(User user)
    {
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();
        // Include the role after saving to ensure it's loaded for token generation
        await _context.Entry(user).Reference(u => u.Role).LoadAsync();
        return user;
    }

    public async Task<User?> GetUserById(int userId)
    {
        return await _context.Users
            .Include(u => u.Role)
            .SingleOrDefaultAsync(x => x.UserID == userId);
    }

    private string CreatePasswordHash(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password);
    }

    private bool VerifyPasswordHash(string password, string passwordHash)
    {
        return BCrypt.Net.BCrypt.Verify(password, passwordHash);
    }

    public async Task<User> GoogleLogin(string googleId, string email, string username, string profileImageURL)
    {
        // Try to find user by GoogleId first, then by email
        var user = await _context.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(x => x.GoogleId == googleId || x.Email == email);

        if (user == null)
        {
            // Create new user if not exists
            var defaultRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "User");
            user = new User
            {
                GoogleId = googleId,
                Email = email,
                Username = username,
                ProfileImageURL = profileImageURL ?? "",
                Bio = "",
                RoleID = defaultRole?.RoleID ?? 1, // Changed from 2 to 1 for User role
                TalentArea = "",
                CreatedAt = DateTime.UtcNow
            };
            await _context.Users.AddAsync(user);
        }
        else if (user.GoogleId == null)
        {
            // Link Google account to existing email account
            user.GoogleId = googleId;
            user.ProfileImageURL = profileImageURL ?? user.ProfileImageURL;
            _context.Users.Update(user);
        }

        await _context.SaveChangesAsync();
        return user;
    }
}
