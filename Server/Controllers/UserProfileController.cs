using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Server.DTOs;
using Server.Interfaces;
using System.Security.Claims;
using Microsoft.Extensions.Logging;

namespace Server.Controllers;

[Authorize]
[Route("api/[controller]")]
[ApiController]
[Produces("application/json")]
public class UserProfileController : ControllerBase
{
    private readonly IUserProfileRepository _userProfileRepository;
    private readonly ILogger<UserProfileController> _logger;

    public UserProfileController(IUserProfileRepository userProfileRepository, ILogger<UserProfileController> logger)
    {
        _userProfileRepository = userProfileRepository;
        _logger = logger;
    }

    [HttpGet("{userId}")]
    [AllowAnonymous] // Temporarily allow anonymous access for testing
    public async Task<ActionResult<UserDTO>> GetProfile(int userId)
    {
        try
        {
            var userDto = await _userProfileRepository.GetUserProfile(userId);
            if (userDto == null)
            {
                return NotFound(new { message = "User not found" });
            }

            return Ok(userDto);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error getting profile: {ex.Message}");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPut("{userId}")]
    public async Task<ActionResult<UserDTO>> UpdateProfile(int userId, [FromBody] UpdateProfileDto updateProfile)
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                return Unauthorized("Invalid token");
            }

            var currentUserId = int.Parse(userIdClaim.Value);

            if (userId != currentUserId)
            {
                return Unauthorized("You can only modify your own profile");
            }

            var userModel = new Server.Models.User
            {
                UserID = userId, // Ensure the ID is set for the update
                Username = updateProfile.Username,
                Email = updateProfile.Email,
                Bio = updateProfile.Bio,
                ProfileImageURL = updateProfile.ProfileImageUrl,
                TalentArea = updateProfile.TalentArea
                // Do not set RoleID or CreatedAt here, as they are not part of the update DTO
            };

            var updatedUser = await _userProfileRepository.UpdateProfile(userModel);

            var userDto = new UserDTO
            {
                UserID = updatedUser.UserID,
                Username = updatedUser.Username,
                Email = updatedUser.Email,
                Bio = updatedUser.Bio,
                ProfileImageURL = updatedUser.ProfileImageURL,
                RoleID = updatedUser.RoleID,
                TalentArea = updatedUser.TalentArea,
                CreatedAt = updatedUser.CreatedAt
            };

            return Ok(userDto);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error updating profile: {ex.Message}");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPut("{userId}/password")]
    public async Task<IActionResult> ChangePassword(int userId, ChangePasswordDto passwordDto)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null)
        {
            return Unauthorized("Invalid token");
        }

        var currentUserId = int.Parse(userIdClaim.Value);
        if (userId != currentUserId)
        {
            return Unauthorized("You can only change your own password");
        }

        var result = await _userProfileRepository.ChangePassword(
            userId,
            passwordDto.CurrentPassword,
            passwordDto.NewPassword
        );

        if (result)
        {
            return Ok(new { message = "Password updated successfully" });
        }

        return BadRequest("Invalid current password");
    }
}
