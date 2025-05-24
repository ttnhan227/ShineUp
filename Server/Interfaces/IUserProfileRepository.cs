using Server.DTOs;
using Server.Models;

namespace Server.Interfaces;

public interface IUserProfileRepository
{
    Task<UserDTO?> GetUserProfile(int userId);

    Task<User> UpdateProfile(User user);

}
