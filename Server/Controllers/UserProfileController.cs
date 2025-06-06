using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Server.DTOs;
using Server.Interfaces;
using System.Security.Claims;
using Microsoft.Extensions.Logging;
using Server.Models;
using Server.Data;
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
    private readonly INotificationRepository _notificationRepository; // Injected Notification Repository
    private readonly DatabaseContext _context; // Database context for direct access

    public UserProfileController(
        IUserProfileRepository userProfileRepository, 
        ICloudinaryService cloudinaryService, 
        ILogger<UserProfileController> logger, 
        IPostRepository postRepository,
        INotificationRepository notificationRepository,
        DatabaseContext context)
    {
        _userProfileRepository = userProfileRepository;
        _cloudinaryService = cloudinaryService; // Assigned
        _logger = logger;
        _postRepository = postRepository; // Assigned
        _notificationRepository = notificationRepository; // Assigned
        _context = context; // Assigned
    }

    // Removed [HttpGet("{userId}")] GetProfile(int userId) action as it's replaced by GetProfileByUsername

    [HttpGet("username/{username}")]
    [AllowAnonymous]
    [Produces("application/json")]
    public async Task<ActionResult> GetProfileByUsername(string username)
    {
        try
        {
            // First, get the current username from the token
            var currentUsername = User.FindFirst(ClaimTypes.Name)?.Value;
            var isCurrentUser = false;
            
            // Get the requested user's profile
            var userDto = await _userProfileRepository.GetUserProfileByUsername(username);
            if (userDto == null)
            {
                return NotFound(new { message = "User not found" });
            }

            // Check if the current user is viewing their own profile
            if (!string.IsNullOrEmpty(currentUsername) && 
                string.Equals(currentUsername, userDto.Username, StringComparison.OrdinalIgnoreCase))
            {
                isCurrentUser = true;
            }
            
            _logger.LogInformation($"Is Current User: {isCurrentUser}, Profile Privacy: {userDto.ProfilePrivacy}");

            // Set IsPrivate flag based on privacy setting and ownership
            userDto.IsPrivate = userDto.ProfilePrivacy == ProfilePrivacy.Private && !isCurrentUser;
            
            // If profile is private and user is not the owner, return limited information
            if (userDto.IsPrivate && !isCurrentUser)
            {
                var result = new Dictionary<string, object>
                {
                    { "IsPrivate", true },
                    { "Username", userDto.Username },
                    { "ProfilePrivacy", userDto.ProfilePrivacy },
                    { "ProfileImageURL", userDto.ProfileImageURL },
                    { "FullName", userDto.FullName },
                    { "IsGoogleAccount", userDto.IsGoogleAccount },
                    { "TalentArea", userDto.TalentArea },
                    { "ProfileCompletionPercentage", userDto.ProfileCompletionPercentage },
                    { "LastLoginTime", userDto.LastLoginTime },
                    { "CreatedAt", userDto.CreatedAt },
                    { "IsActive", userDto.IsActive },
                    { "Verified", userDto.Verified }
                };

                // Only include Email and Bio if they are not null
                if (userDto.Email != null)
                {
                    result.Add("Email", userDto.Email);
                }
                
                if (userDto.Bio != null)
                {
                    result.Add("Bio", userDto.Bio);
                }

                return Ok(result);
            }

            // Ensure the profile image URL is fully qualified
            if (!string.IsNullOrEmpty(userDto.ProfileImageURL))
            {
                userDto.ProfileImageURL = EnsureFullImageUrl(userDto.ProfileImageURL);
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
                updateProfile.ProfileImageUrl = EnsureFullImageUrl(uploadResult.SecureUrl.ToString());
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

            // Create a notification for profile update
            try
            {
                var notificationDto = new CreateNotificationDTO
                {
                    UserID = currentUserId,
                    Message = "Your profile has been successfully updated.",
                    Type = NotificationType.Generic,
                    RelatedEntityType = "Profile"
                };

                await _notificationRepository.CreateNotificationAsync(notificationDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating profile update notification");
                // Don't fail the profile update if notification fails
            }

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

            // Get user info before changing password for the notification
            var user = await _context.Users.FindAsync(currentUserId);
            
            var success = await _userProfileRepository.ChangePassword(
                currentUserId,
                changePasswordDto.CurrentPassword,
                changePasswordDto.NewPassword
            );

            if (success)
            {
                // Create a notification for password change
                try
                {
                    var notificationDto = new CreateNotificationDTO
                    {
                        UserID = currentUserId,
                        Message = "Your password was recently changed. If you didn't make this change, please secure your account.",
                        Type = NotificationType.Security,
                        RelatedEntityType = "Account"
                    };

                    await _notificationRepository.CreateNotificationAsync(notificationDto);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating password change notification");
                    // Don't fail the password change if notification fails
                }

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
    // In Server/Controllers/UserProfileController.cs
    private string EnsureFullImageUrl(string imageUrl)
    {
        if (string.IsNullOrEmpty(imageUrl))
            return imageUrl;

        // If it's already a full URL, return as is
        if (imageUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || 
            imageUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            return imageUrl;
        }

        // If it's a Cloudinary URL without scheme, add https://
        if (imageUrl.StartsWith("res.cloudinary.com", StringComparison.OrdinalIgnoreCase))
        {
            return "https://" + imageUrl;
        }

        // For relative URLs, you might want to prepend your site's base URL
        // return $"{Request.Scheme}://{Request.Host}{imageUrl.TrimStart('~')}";
    
        return imageUrl; // Fallback
    }
}
