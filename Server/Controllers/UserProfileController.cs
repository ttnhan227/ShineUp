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
                _logger.LogWarning("No NameIdentifier claim found in token");
                return Unauthorized("Invalid token");
            }

            var currentUserId = int.Parse(userIdClaim.Value);
            _logger.LogInformation($"Token UserId: {currentUserId}, Requested UserId: {userId}");

            if (userId != currentUserId)
            {
                _logger.LogWarning($"User {currentUserId} attempted to modify profile of user {userId}");
                return Unauthorized("You can only modify your own profile");
            }

            var updatedUser = await _userProfileRepository.UpdateProfile(
                userId,
                updateProfile.Username,
                updateProfile.Email,
                updateProfile.Bio,
                updateProfile.ProfileImageUrl,
                updateProfile.TalentArea
            );

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
            _logger.LogWarning("No NameIdentifier claim found in token");
            return Unauthorized("Invalid token");
        }

        var currentUserId = int.Parse(userIdClaim.Value);
        if (userId != currentUserId)
        {
            _logger.LogWarning($"User {currentUserId} attempted to change password of user {userId}");
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