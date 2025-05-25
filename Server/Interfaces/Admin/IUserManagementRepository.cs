using Server.DTOs;
using Server.Models;

namespace Server.Interfaces.Admin;

public interface IUserManagementRepository
{
    Task<IEnumerable<UserDTO>> GetAllUsers();
    Task<UserDTO?> GetUserById(int userId);
    Task<UserDTO?> UpdateUserRole(int userId, int roleId);
    Task<UserDTO?> UpdateUserStatus(int userId, string field, bool value);
    Task<bool> DeleteUser(int userId);
} 