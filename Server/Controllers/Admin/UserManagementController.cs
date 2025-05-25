using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Server.DTOs;
using Server.DTOs.Admin;
using Server.Interfaces.Admin;
using System.Security.Claims;

namespace Server.Controllers.Admin;

[Authorize(Roles = "Admin")]
[Route("api/admin/[controller]")]
[ApiController]
public class UserManagementController : ControllerBase
{
    private readonly IUserManagementRepository _userManagementRepository;
    private readonly ILogger<UserManagementController> _logger;
    private const int ADMIN_ROLE_ID = 2;

    public UserManagementController(
        IUserManagementRepository userManagementRepository,
        ILogger<UserManagementController> logger)
    {
        _userManagementRepository = userManagementRepository;
        _logger = logger;
    }

    private bool IsAdmin()
    {
        var roleIdClaim = User.FindFirst("RoleID");
        return roleIdClaim != null && int.TryParse(roleIdClaim.Value, out int roleId) && roleId == ADMIN_ROLE_ID;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<UserDTO>>> GetAllUsers()
    {
        if (!IsAdmin())
        {
            return Forbid();
        }

        try
        {
            var users = await _userManagementRepository.GetAllUsers();
            return Ok(users);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error getting all users: {ex.Message}");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpGet("{userId}")]
    public async Task<ActionResult<UserDTO>> GetUser(int userId)
    {
        if (!IsAdmin())
        {
            return Forbid();
        }

        try
        {
            var user = await _userManagementRepository.GetUserById(userId);
            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }
            return Ok(user);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error getting user: {ex.Message}");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPut("{userId}/role")]
    public async Task<ActionResult<UserDTO>> UpdateUserRole(int userId, [FromBody] UpdateUserRoleDTO roleUpdate)
    {
        if (!IsAdmin())
        {
            return Forbid();
        }

        try
        {
            var user = await _userManagementRepository.UpdateUserRole(userId, roleUpdate.RoleID);
            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }
            return Ok(user);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error updating user role: {ex.Message}");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpDelete("{userId}")]
    public async Task<IActionResult> DeleteUser(int userId)
    {
        if (!IsAdmin())
        {
            return Forbid();
        }

        try
        {
            var success = await _userManagementRepository.DeleteUser(userId);
            if (!success)
            {
                return NotFound(new { message = "User not found" });
            }
            return Ok(new { message = "User deleted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error deleting user: {ex.Message}");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }
} 