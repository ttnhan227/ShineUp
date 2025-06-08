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

        // A user is considered a Google account if they have a GoogleId or no password hash
        var isGoogleAccount = !string.IsNullOrEmpty(user.GoogleId) || string.IsNullOrEmpty(user.PasswordHash);
        _logger.LogInformation($"User {user.UserID} - GoogleId: {user.GoogleId}, PasswordHash null/empty: {string.IsNullOrEmpty(user.PasswordHash)}, IsGoogleAccount: {isGoogleAccount}");

        return new UserDTO
        {
            UserID = user.UserID,
            Username = user.Username,
            FullName = user.FullName,
            Email = user.Email,
            Bio = user.Bio,
            ProfileImageURL = user.ProfileImageURL,
            RoleID = user.RoleID,
            TalentArea = user.TalentArea,
            CreatedAt = user.CreatedAt,
            IsActive = user.IsActive,
            Verified = user.Verified,
            LastLoginTime = user.LastLoginTime, // Already in UTC from AuthRepository
            ProfilePrivacy = user.ProfilePrivacy,
            ProfileCompletionPercentage = completionPercentage,
            IsGoogleAccount = isGoogleAccount,
            InstagramUrl = user.InstagramUrl,
            YouTubeUrl = user.YouTubeUrl,
            TwitterUrl = user.TwitterUrl,
            CoverPhotoUrl = user.CoverPhotoUrl
        };
    }

    private int CalculateProfileCompletion(User user)
    {
        // Define the fields that count towards profile completion
        var completedFields = 0;
        var totalFields = 10; // Total number of fields that count towards completion

        // Check each field and increment completedFields if it's filled
        if (!string.IsNullOrWhiteSpace(user.Username)) completedFields++;
        if (!string.IsNullOrWhiteSpace(user.FullName)) completedFields++;
        // Email verification counts as one field
        if (user.Verified) completedFields++;
        if (!string.IsNullOrWhiteSpace(user.Bio)) completedFields++;
        if (!string.IsNullOrWhiteSpace(user.ProfileImageURL)) completedFields++;
        if (!string.IsNullOrWhiteSpace(user.TalentArea)) completedFields++;
        // Social media links (count each one)
        if (!string.IsNullOrWhiteSpace(user.InstagramUrl)) completedFields++;
        if (!string.IsNullOrWhiteSpace(user.YouTubeUrl)) completedFields++;
        if (!string.IsNullOrWhiteSpace(user.TwitterUrl)) completedFields++;
        // Cover photo
        if (!string.IsNullOrWhiteSpace(user.CoverPhotoUrl)) completedFields++;

        // Calculate percentage (ensure it doesn't exceed 100%)
        var percentage = (int)((double)completedFields / totalFields * 100);
        return Math.Min(percentage, 100); // Cap at 100%
    }

    public async Task<UserDTO?> GetUserProfileByUsername(string username)
    {
        var user = await _context.Users
            .Include(u => u.Role)
            .SingleOrDefaultAsync(x => x.Username == username);

        if (user == null) return null;

        // Calculate profile completion percentage
        var completionPercentage = CalculateProfileCompletion(user);

        // A user is considered a Google account if they have a GoogleId or no password hash
        var isGoogleAccount = !string.IsNullOrEmpty(user.GoogleId) || string.IsNullOrEmpty(user.PasswordHash);
        _logger.LogInformation($"User {user.UserID} (Username: {user.Username}) - GoogleId: {user.GoogleId}, PasswordHash null/empty: {string.IsNullOrEmpty(user.PasswordHash)}, IsGoogleAccount: {isGoogleAccount}");

        return new UserDTO
        {
            UserID = user.UserID,
            Username = user.Username,
            FullName = user.FullName,
            Email = user.Email,
            Bio = user.Bio,
            ProfileImageURL = user.ProfileImageURL,
            RoleID = user.RoleID,
            TalentArea = user.TalentArea,
            CreatedAt = user.CreatedAt,
            IsActive = user.IsActive,
            Verified = user.Verified,
            LastLoginTime = user.LastLoginTime, // Already in UTC from AuthRepository
            ProfilePrivacy = user.ProfilePrivacy,
            ProfileCompletionPercentage = completionPercentage,
            IsGoogleAccount = isGoogleAccount,
            InstagramUrl = user.InstagramUrl,
            YouTubeUrl = user.YouTubeUrl,
            TwitterUrl = user.TwitterUrl,
            CoverPhotoUrl = user.CoverPhotoUrl
        };
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
        if (!string.IsNullOrEmpty(userToUpdate.FullName))
        {
            existingUser.FullName = userToUpdate.FullName;
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

        // Update social media links if provided
        if (userToUpdate.InstagramUrl != null)
        {
            existingUser.InstagramUrl = userToUpdate.InstagramUrl;
        }

        if (userToUpdate.YouTubeUrl != null)
        {
            existingUser.YouTubeUrl = userToUpdate.YouTubeUrl;
        }

        if (userToUpdate.TwitterUrl != null)
        {
            existingUser.TwitterUrl = userToUpdate.TwitterUrl;
        }

        // Update cover photo if provided
        if (userToUpdate.CoverPhotoUrl != null)
        {
            existingUser.CoverPhotoUrl = userToUpdate.CoverPhotoUrl;
        }

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
