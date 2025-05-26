using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Server.Data;
using Server.DTOs;
using Server.Interfaces;
using Server.Models;
using BCrypt.Net; // Assuming BCrypt.Net is installed

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

        // Calculate profile completion percentage
        var completionPercentage = CalculateProfileCompletion(user);

        return new UserDTO
        {
            UserID = user.UserID,
            Username = user.Username,
            Email = user.Email,
            Bio = user.Bio,
            ProfileImageURL = user.ProfileImageURL,
            RoleID = user.RoleID,
            TalentArea = user.TalentArea,
            CreatedAt = user.CreatedAt,
            IsActive = user.IsActive,
            Verified = user.Verified,
            LastLoginTime = user.LastLoginTime,
            ProfilePrivacy = user.ProfilePrivacy,
            ProfileCompletionPercentage = completionPercentage
        };
    }

    private int CalculateProfileCompletion(User user)
    {
        var totalFields = 5; // Total number of fields to check
        var completedFields = 0;

        // Check each field and increment completedFields if it's filled
        if (!string.IsNullOrWhiteSpace(user.Username)) completedFields++;
        if (!string.IsNullOrWhiteSpace(user.Email)) completedFields++;
        if (!string.IsNullOrWhiteSpace(user.Bio)) completedFields++;
        if (!string.IsNullOrWhiteSpace(user.ProfileImageURL)) completedFields++;
        if (!string.IsNullOrWhiteSpace(user.TalentArea)) completedFields++;

        // Calculate percentage
        return (int)((double)completedFields / totalFields * 100);
    }

    public async Task<User> UpdateProfile(User userToUpdate)
    {
        var existingUser = await _context.Users
            .AsTracking()
            .FirstOrDefaultAsync(u => u.UserID == userToUpdate.UserID);

        if (existingUser == null)
        {
            throw new Exception("User not found");
        }

        // Apply updates from userToUpdate to existingUser
        if (!string.IsNullOrEmpty(userToUpdate.Username))
        {
            existingUser.Username = userToUpdate.Username;
        }

        if (!string.IsNullOrEmpty(userToUpdate.Email))
        {
            existingUser.Email = userToUpdate.Email;
        }

        if (userToUpdate.Bio != null)
        {
            existingUser.Bio = userToUpdate.Bio;
        }

        if (userToUpdate.ProfileImageURL != null)
        {
            existingUser.ProfileImageURL = userToUpdate.ProfileImageURL;
        }

        if (userToUpdate.TalentArea != null)
        {
            existingUser.TalentArea = userToUpdate.TalentArea;
        }

        // Update profile privacy if provided
        existingUser.ProfilePrivacy = userToUpdate.ProfilePrivacy;

        try
        {
            _context.Entry(existingUser).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            
            // Reload the user to ensure we have the latest data
            await _context.Entry(existingUser).ReloadAsync();
            return existingUser;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error updating user: {ex.Message}");
            throw;
        }
    }

    public async Task<bool> ChangePassword(int userId, string currentPassword, string newPassword)
    {
        var user = await _context.Users.SingleOrDefaultAsync(u => u.UserID == userId);

        if (user == null)
        {
            _logger.LogWarning($"ChangePassword failed: User with ID {userId} not found.");
            return false;
        }

        // Verify current password
        if (!BCrypt.Net.BCrypt.Verify(currentPassword, user.PasswordHash))
        {
            _logger.LogWarning($"ChangePassword failed for user {userId}: Incorrect current password.");
            return false;
        }

        // Hash the new password
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);

        try
        {
            _context.Entry(user).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            _logger.LogInformation($"Password successfully changed for user {userId}.");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error changing password for user {userId}: {ex.Message}");
            return false;
        }
    }
}
