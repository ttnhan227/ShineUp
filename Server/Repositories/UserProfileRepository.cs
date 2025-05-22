using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Server.Data;
using Server.DTOs;
using Server.Interfaces;
using Server.Models;

namespace Server.Repositories;

public class UserProfileRepository : IUserProfileRepository
{
    private readonly DatabaseContext _context;
    private readonly ILogger<UserProfileRepository> _logger;

    public UserProfileRepository(DatabaseContext context, ILogger<UserProfileRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<UserDTO?> GetUserProfile(int userId)
    {
        var user = await _context.Users
            .Include(u => u.Role)
            .SingleOrDefaultAsync(x => x.UserID == userId);

        if (user == null) return null;

        return new UserDTO
        {
            UserID = user.UserID,
            Username = user.Username,
            Email = user.Email,
            Bio = user.Bio,
            ProfileImageURL = user.ProfileImageURL,
            RoleID = user.RoleID,
            TalentArea = user.TalentArea,
            CreatedAt = user.CreatedAt
        };
    }

    public async Task<User> UpdateProfile(int userId, string? username, string? email, string? bio,
        string? profileImageUrl, string? talentArea)
    {
        _logger.LogInformation($"Attempting to update profile for user {userId}");
        
        var user = await _context.Users
            .AsTracking()  // Explicitly enable tracking
            .FirstOrDefaultAsync(u => u.UserID == userId);
            
        if (user == null)
        {
            _logger.LogWarning($"User {userId} not found");
            throw new Exception("User not found");
        }

        _logger.LogInformation($"Current user state: {System.Text.Json.JsonSerializer.Serialize(user)}");

        if (!string.IsNullOrEmpty(username))
        {
            _logger.LogInformation($"Updating username from {user.Username} to {username}");
            user.Username = username;
        }

        if (!string.IsNullOrEmpty(email))
        {
            _logger.LogInformation($"Updating email from {user.Email} to {email}");
            user.Email = email;
        }

        if (bio != null)
        {
            _logger.LogInformation($"Updating bio from {user.Bio} to {bio}");
            user.Bio = bio;
        }

        if (profileImageUrl != null)
        {
            _logger.LogInformation($"Updating profile image from {user.ProfileImageURL} to {profileImageUrl}");
            user.ProfileImageURL = profileImageUrl;
        }

        if (talentArea != null)
        {
            _logger.LogInformation($"Updating talent area from {user.TalentArea} to {talentArea}");
            user.TalentArea = talentArea;
        }

        try
        {
            _context.Entry(user).State = EntityState.Modified;
            var result = await _context.SaveChangesAsync();
            _logger.LogInformation($"SaveChanges result: {result} rows affected");
            
            // Reload the user to ensure we have the latest data
            await _context.Entry(user).ReloadAsync();
            return user;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error updating user: {ex.Message}");
            throw;
        }
    }

    public async Task<bool> ChangePassword(int userId, string currentPassword, string newPassword)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
        {
            return false;
        }

        if (!BCrypt.Net.BCrypt.Verify(currentPassword, user.PasswordHash))
        {
            return false;
        }

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        await _context.SaveChangesAsync();
        return true;
    }
}