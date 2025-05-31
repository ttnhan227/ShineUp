using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Server.DTOs;
using Server.Interfaces;
using System.Security.Claims;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

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
    private readonly IPostRepository _postRepository; // Injected Post Repository

    public UserProfileController(IUserProfileRepository userProfileRepository, ICloudinaryService cloudinaryService, ILogger<UserProfileController> logger, IPostRepository postRepository)
    {
        _userProfileRepository = userProfileRepository;
        _cloudinaryService = cloudinaryService; // Assigned
        _logger = logger;
        _postRepository = postRepository; // Assigned
    }

    // Removed [HttpGet("{userId}")] GetProfile(int userId) action as it's replaced by GetProfileByUsername

    [HttpGet("username/{username}")]
    [AllowAnonymous]
    [Produces("application/json")]
    public async Task<ActionResult<UserDTO>> GetProfileByUsername(string username)
    {
        try
        {
            var userDto = await _userProfileRepository.GetUserProfileByUsername(username);
            if (userDto == null)
            {
                return NotFound(new { message = "User not found" });
            }

            return Ok(userDto);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error getting profile by username {username}: {ex.Message}");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPut("update")] // Route changed, userId comes from token
    [Consumes("multipart/form-data")]
    [Produces("application/json")]
    public async Task<ActionResult<UserDTO>> UpdateProfile([FromForm] UpdateProfileDto updateProfile)
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                return Unauthorized("Invalid token: User ID claim not found.");
            }

            if (!int.TryParse(userIdClaim.Value, out var currentUserId))
            {
                 return Unauthorized("Invalid token: User ID claim is not a valid integer.");
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

            // Fetch existing user by currentUserId from token to ensure we're updating the correct user
            var existingUserDto = await _userProfileRepository.GetUserProfile(currentUserId);
            if (existingUserDto == null)
            {
                return NotFound(new { message = "User not found" });
            }

            var userModel = new Server.Models.User
            {
                UserID = currentUserId, // Use ID from token
                Username = updateProfile.Username,
                FullName = updateProfile.FullName,
                Email = updateProfile.Email,
                Bio = updateProfile.Bio,
                ProfileImageURL = updateProfile.ProfileImageUrl,
                TalentArea = updateProfile.TalentArea,
                ProfilePrivacy = updateProfile.ProfilePrivacy ?? existingUserDto.ProfilePrivacy
            };

            var updatedUser = await _userProfileRepository.UpdateProfile(userModel);

            var userDto = new UserDTO
            {
                UserID = updatedUser.UserID,
                Username = updatedUser.Username,
                FullName = updatedUser.FullName,
                Email = updatedUser.Email,
                Bio = updatedUser.Bio,
                ProfileImageURL = updatedUser.ProfileImageURL,
                RoleID = updatedUser.RoleID,
                TalentArea = updatedUser.TalentArea,
                CreatedAt = updatedUser.CreatedAt,
                IsActive = updatedUser.IsActive, // ensure all relevant fields are mapped
                Verified = updatedUser.Verified,
                LastLoginTime = updatedUser.LastLoginTime,
                ProfilePrivacy = updatedUser.ProfilePrivacy,
                ProfileCompletionPercentage = existingUserDto.ProfileCompletionPercentage, // Recalculate or carry over
                IsGoogleAccount = existingUserDto.IsGoogleAccount
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

    [HttpGet("username/{username}/posts")] // Route changed to use username
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<PostListResponseDto>>> GetUserPosts(string username)
    {
        try
        {
            var userDto = await _userProfileRepository.GetUserProfileByUsername(username);
            if (userDto == null)
            {
                return NotFound(new { message = $"User '{username}' not found" });
            }

            var posts = await _postRepository.GetPostsByUserIdAsync(userDto.UserID);
            var postDtos = posts.Select(p => new PostListResponseDto
            {
                PostID = p.PostID,
                Title = p.Title,
                Content = p.Content,
                CreatedAt = p.CreatedAt,
                UserID = p.UserID,
                UserName = p.User.Username,
                FullName = p.User.FullName,
                ProfileImageURL = p.User.ProfileImageURL,
                CategoryName = p.Category?.CategoryName,
                LikesCount = p.Likes?.Count ?? 0,
                CommentsCount = p.Comments?.Count ?? 0,
                MediaFiles = p.Images.Select(i => new MediaFileDTO
                {
                    Url = i.ImageURL?.Replace("http://", "https://"),
                    Type = "image",
                    PublicId = i.CloudPublicId
                }).Concat(p.Videos.Select(v => new MediaFileDTO
                {
                    Url = v.VideoURL?.Replace("http://", "https://"),
                    Type = "video",
                    PublicId = v.CloudPublicId
                })).ToList()
            });

            return Ok(postDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error getting user posts: {ex.Message}");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }
}
