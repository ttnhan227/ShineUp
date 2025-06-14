using Server.DTOs;
using Server.Models;

namespace Server.Interfaces;

public interface IUserProfileRepository
{
    Task<UserDTO?> GetUserProfile(int userId);

    Task<UserDTO?> GetUserProfileByUsername(string username);
    Task<User> UpdateProfile(User user);
    Task<bool> ChangePassword(int userId, string currentPassword, string newPassword);
}