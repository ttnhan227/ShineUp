using Server.DTOs;
using Server.Models;

namespace Server.Interfaces;

public interface IUserProfileRepository
{
    Task<UserDTO?> GetUserProfile(int userId);

    Task<User> UpdateProfile(int userId, string? username, string? email, string? bio, string? profileImageUrl,
        string? talentArea);

    Task<bool> ChangePassword(int userId, string currentPassword, string newPassword);
}