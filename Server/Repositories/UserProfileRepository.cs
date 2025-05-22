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
