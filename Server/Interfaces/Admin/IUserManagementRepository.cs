using Server.DTOs;
using Server.Models;

namespace Server.Interfaces.Admin;

public interface IUserManagementRepository
{
    Task<IEnumerable<UserDTO>> GetAllUsers();
    Task<UserDTO?> GetUserById(int userId);
    Task<UserDTO?> UpdateUserRole(int userId, int roleId);
    Task<bool> DeleteUser(int userId);
} 