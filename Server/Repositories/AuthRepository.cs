using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Server.Data;
using Server.Interfaces;
using Server.Models;

namespace Server.Repositories;

public class AuthRepository : IAuthRepository
{
    private readonly DatabaseContext _context;
    private readonly ILogger<AuthRepository> _logger;

    public AuthRepository(DatabaseContext context, ILogger<AuthRepository> logger)
    {
        _context = context;
        _logger = logger;
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

    public async Task<User> Login(string emailOrUsername, string password)
    {
        var user = await _context.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(x => x.Email.Equals(emailOrUsername) || x.Username.Equals(emailOrUsername));

        if (user == null)
        {
            return null;
        }

        if (!user.IsActive)
        {
            return null;
        }

        // Check if user has a password hash (not a Google user)
        if (string.IsNullOrEmpty(user.PasswordHash))
        {
            _logger.LogWarning($"User {user.Email} has no password hash (possibly a Google user)");
            return null;
        }

        try
        {
            if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            {
                return null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error verifying password for user {user.Email}: {ex.Message}");
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

    private async Task<string> GenerateUniqueUsername(string baseUsername)
    {
        string username = baseUsername;
        int counter = 1;

        while (await _context.Users.AnyAsync(u => u.Username == username))
        {
            username = $"{baseUsername}{counter++}";
        }

        return username;
    }

    public async Task<string> GenerateOTP(string email)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (user == null) throw new Exception("User not found");

        var otp = new Random().Next(100000, 999999).ToString();
        var otpModel = new ForgetPasswordOTP
        {
            Email = email,
            OTPCode = BCrypt.Net.BCrypt.HashPassword(otp),
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddMinutes(15),
            IsUsed = false
        };

        _context.ForgetPasswordOTPs.Add(otpModel);
        await _context.SaveChangesAsync();

        return otp;
    }

    public async Task<bool> ValidateOTP(string email, string otp)
    {
        var otpModel = await _context.ForgetPasswordOTPs
            .Where(o => o.Email == email && !o.IsUsed && o.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(o => o.CreatedAt)
            .FirstOrDefaultAsync();

        if (otpModel == null)
        {
            // Log for debugging
            _logger.LogWarning($"No valid OTP found for email: {email}");
            return false;
        }

        bool isValid = BCrypt.Net.BCrypt.Verify(otp, otpModel.OTPCode);
        
        if (isValid)
        {
            otpModel.IsUsed = true;
            await _context.SaveChangesAsync();
            return true;
        }

        // Log for debugging
        _logger.LogWarning($"OTP validation failed for email: {email}");
        return false;
    }

    public async Task<bool> ResetPassword(string email, string newPassword)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (user == null) return false;

        // Check if new password is same as old password
        if (user.PasswordHash != null && BCrypt.Net.BCrypt.Verify(newPassword, user.PasswordHash))
        {
            throw new Exception("New password cannot be the same as your old password");
        }

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> SaveOTP(int userId, string otp)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
        {
            _logger.LogWarning($"User not found for ID: {userId}");
            return false;
        }

        var otpModel = new ForgetPasswordOTP
        {
            Email = user.Email,
            OTPCode = BCrypt.Net.BCrypt.HashPassword(otp),
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddMinutes(15),
            IsUsed = false,
            UserID = userId
        };

        _context.ForgetPasswordOTPs.Add(otpModel);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> VerifyOTP(int userId, string otp)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null) return false;

        var otpModel = await _context.ForgetPasswordOTPs
            .Where(o => o.Email == user.Email && !o.IsUsed && o.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(o => o.CreatedAt)
            .FirstOrDefaultAsync();

        if (otpModel == null)
        {
            _logger.LogWarning($"No valid OTP found for user ID: {userId}");
            return false;
        }

        bool isValid = BCrypt.Net.BCrypt.Verify(otp, otpModel.OTPCode);
        
        if (isValid)
        {
            otpModel.IsUsed = true;
            await _context.SaveChangesAsync();
            return true;
        }

        _logger.LogWarning($"OTP validation failed for user ID: {userId}");
        return false;
    }
}
