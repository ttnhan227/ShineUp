using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.DTOs;
using Server.Interfaces.Admin;
using Server.Models;

namespace Server.Repositories.Admin;

public class UserManagementRepository : IUserManagementRepository
{
    private readonly DatabaseContext _context;

    public UserManagementRepository(DatabaseContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<UserDTO>> GetAllUsers()
    {
        return await _context.Users
            .Include(u => u.Role)
            .Select(u => new UserDTO
            {
                UserID = u.UserID,
                Username = u.Username,
                Email = u.Email,
                Bio = u.Bio,
                ProfileImageURL = u.ProfileImageURL,
                RoleID = u.RoleID,
                TalentArea = u.TalentArea,
                CreatedAt = u.CreatedAt
            })
            .ToListAsync();
    }

    public async Task<UserDTO?> GetUserById(int userId)
    {
        var user = await _context.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.UserID == userId);

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

    public async Task<UserDTO?> UpdateUserRole(int userId, int roleId)
    {
        var user = await _context.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.UserID == userId);

        if (user == null) return null;

        user.RoleID = roleId;
        await _context.SaveChangesAsync();

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

    public async Task<bool> DeleteUser(int userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null) return false;

        _context.Users.Remove(user);
        await _context.SaveChangesAsync();
        return true;
    }
} 