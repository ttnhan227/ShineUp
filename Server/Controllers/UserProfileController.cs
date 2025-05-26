using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Server.DTOs;
using Server.Interfaces;
using System.Security.Claims;
using Microsoft.Extensions.Logging;
using System; // Added for Exception

namespace Server.Controllers;

[Authorize]
[Route("api/[controller]")]
[ApiController]
// Removed [Produces("application/json")] from controller level to allow multipart/form-data
public class UserProfileController : ControllerBase
{
    private readonly IUserProfileRepository _userProfileRepository;
    private readonly ICloudinaryService _cloudinaryService; // Injected Cloudinary Service
    private readonly ILogger<UserProfileController> _logger;

    public UserProfileController(IUserProfileRepository userProfileRepository, ICloudinaryService cloudinaryService, ILogger<UserProfileController> logger)
    {
        _userProfileRepository = userProfileRepository;
        _cloudinaryService = cloudinaryService; // Assigned
        _logger = logger;
    }

    [HttpGet("{userId}")]
    [AllowAnonymous] // Temporarily allow anonymous access for testing
    [Produces("application/json")] // Added back for this specific action
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
    [Consumes("multipart/form-data")] // Consume multipart form data for file upload
    [Produces("application/json")] // Produce JSON for response
    public async Task<ActionResult<UserDTO>> UpdateProfile(int userId, [FromForm] UpdateProfileDto updateProfile) // Changed to FromForm
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

            // Handle image upload if a file is provided
            if (updateProfile.ProfileImageFile != null && updateProfile.ProfileImageFile.Length > 0)
            {
                var uploadResult = await _cloudinaryService.UploadImgAsync(updateProfile.ProfileImageFile);
                if (uploadResult.Error != null)
                {
                    _logger.LogError($"Cloudinary image upload error: {uploadResult.Error.Message}");
                    return BadRequest(new { message = "Image upload failed: " + uploadResult.Error.Message });
                }
                updateProfile.ProfileImageUrl = uploadResult.SecureUrl.ToString();
            }

            var existingUser = await _userProfileRepository.GetUserProfile(userId);
            if (existingUser == null)
            {
                return NotFound(new { message = "User not found" });
            }

            var userModel = new Server.Models.User
            {
                UserID = userId, // Ensure the ID is set for the update
                Username = updateProfile.Username,
                Email = updateProfile.Email,
                Bio = updateProfile.Bio,
                ProfileImageURL = updateProfile.ProfileImageUrl, // Use the potentially new URL
                TalentArea = updateProfile.TalentArea,
                ProfilePrivacy = updateProfile.ProfilePrivacy ?? existingUser.ProfilePrivacy // Use the value from DTO, fallback to existing
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

    [HttpPost("ChangePassword")]
    [Consumes("application/json")]
    [Produces("application/json")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDTO changePasswordDto)
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                return Unauthorized(new { message = "Invalid token or user ID not found." });
            }

            var currentUserId = int.Parse(userIdClaim.Value);

            // The DTO doesn't contain userId, so we use the one from the claim.
            // No need to compare route userId with claim userId here as there's no route parameter for userId.

            var success = await _userProfileRepository.ChangePassword(
                currentUserId,
                changePasswordDto.CurrentPassword,
                changePasswordDto.NewPassword
            );

            if (success)
            {
                return Ok(new { message = "Password changed successfully." });
            }
            else
            {
                // This could be due to incorrect current password or user not found (though user not found should be caught by Unauthorized above)
                return BadRequest(new { message = "Failed to change password. Please check your current password." });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error changing password: {ex.Message}");
            return StatusCode(500, new { message = "Internal server error." });
        }
    }
}
