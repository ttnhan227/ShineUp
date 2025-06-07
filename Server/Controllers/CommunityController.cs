using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Server.DTOs;
using Server.Interfaces;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Models;

namespace Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CommunityController : ControllerBase
{
    private readonly ICommunityRepository _communityRepository;
    private readonly ILogger<CommunityController> _logger;
    private readonly DatabaseContext _db;

    public CommunityController(ICommunityRepository communityRepository, ILogger<CommunityController> logger,
        DatabaseContext db)
    {
        _communityRepository = communityRepository;
        _logger = logger;
        _db = db;
    }

    [HttpGet("privacy-options")]
    public async Task<ActionResult<IEnumerable<Privacy>>> GetAllowedPrivacyOptions()
    {
        return await _db.Privacies
            .Where(p => p.PrivacyID == 1 || p.PrivacyID == 3)
            .ToListAsync();
    }

    
    /// Lấy thông tin chi tiết của một cộng đồng theo ID
    /// Bao gồm: thông tin cơ bản, danh sách thành viên, vai trò của user hiện tại
    [HttpGet("{communityId}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetCommunityById(int communityId)
    {
        try
        {
            int userId = 0; // Default to 0 for unauthenticated users
            
            try 
            {
                userId = GetUserId(); // Lấy userId từ token nếu có
            }
            catch (UnauthorizedAccessException)
            {
                // User không đăng nhập, sử dụng userId = 0
            }
            
            if (communityId <= 0)
                return BadRequest("Invalid community ID.");
                
            var community = await _communityRepository.GetCommunityDetailsAsync(communityId, userId);
            return Ok(community);
        }
        catch (KeyNotFoundException)
        {
            return NotFound("Community not found.");
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized("You need to be logged in to view this community.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve community with ID {CommunityId}", communityId);
            return StatusCode(500, "Internal server error while retrieving community details.");
        }
    }

    [HttpPost]
    [Authorize]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> CreateCommunity([FromForm] CreateCommunityDTO dto)
    {
        try
        {
            int userId = GetUserId();
            var result = await _communityRepository.CreateCommunityAsync(dto, userId);
            return Ok(result);
        }
        catch
        {
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetAllCommunities()
    {
        try
        {
            var result = await _communityRepository.GetAllCommunitiesAsync();
            return Ok(result);
        }
        catch
        {
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPost("{communityId}/join")]
    [Authorize]
    public async Task<IActionResult> JoinCommunity(int communityId)
    {
        try
        {
            int userId = GetUserId();
            await _communityRepository.JoinCommunityAsync(communityId, userId);
            return NoContent();
        }
        catch (Exception ex)
        {
            return BadRequest("Failed to join community");
        }
    }

    [HttpPost("{communityId}/leave")]
    [Authorize]
    public async Task<IActionResult> LeaveCommunity(int communityId)
    {
        try
        {
            int userId = GetUserId();
            await _communityRepository.LeaveCommunityAsync(communityId, userId);
            return NoContent();
        }
        catch (Exception)
        {
            return BadRequest("Failed to leave community");
        }
    }

    [HttpPost("{communityId}/transfer-moderator")]
    [Authorize]
    public async Task<IActionResult> TransferModerator(int communityId, [FromQuery] int newModeratorId)
    {
        if (communityId <= 0 || newModeratorId <= 0)
            return BadRequest("Invalid community or user ID");

        try
        {
            int currentModeratorId = GetUserId();
            
            // Verify current user is a member and moderator of the community
            bool isMember = await _communityRepository.IsUserMemberAsync(communityId, currentModeratorId);
            bool isModerator = await _communityRepository.IsUserModeratorAsync(communityId, currentModeratorId);
            
            if (!isMember || !isModerator)
                return Forbid("You don't have permission to perform this action");

            // Verify new moderator is a member of the community
            bool isNewModeratorMember = await _communityRepository.IsUserMemberAsync(communityId, newModeratorId);
            if (!isNewModeratorMember)
                return BadRequest("The specified user is not a member of this community");

            await _communityRepository.TransferModeratorAsync(communityId, currentModeratorId, newModeratorId);
            return NoContent();
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument during moderator transfer for community {CommunityId}", communityId);
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "TransferModerator failed for community {CommunityId}", communityId);
            return StatusCode(500, "An error occurred while transferring moderator role.");
        }
    }

    [HttpGet("{communityId}/members")]
    public async Task<IActionResult> GetCommunityMembers(int communityId)
    {
        try
        {
            var members = await _communityRepository.GetCommunityMembersAsync(communityId);
            return Ok(members);
        }
        catch
        {
            return StatusCode(500, "Failed to retrieve community members");
        }
    }

    [HttpGet("{communityId}/posts")]
    public async Task<IActionResult> GetCommunityPosts(int communityId)
    {
        try
        {
            var posts = await _communityRepository.GetCommunityPostsAsync(communityId);
            return Ok(posts);
        }
        catch
        {
            return StatusCode(500, "Failed to retrieve community posts");
        }
    }

    [HttpPut("{communityId}")]
    [Authorize]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UpdateCommunity(int communityId, [FromForm] UpdateCommunityDTO dto)
    {
        try
        {
            var userId = GetUserId();
            
            // Check if user is Moderator of this community
            if (!await IsUserModeratorAsync(communityId, userId))
            {
                return Forbid("You must be an Moderator to update this community.");
            }
            
            // Ensure the community ID in the path matches the DTO
            if (communityId != dto.CommunityID)
            {
                return BadRequest("Community ID in the path does not match the request body.");
            }
            
            var result = await _communityRepository.UpdateCommunityAsync(communityId, dto, userId);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update community {CommunityId}", communityId);
            return StatusCode(500, "An error occurred while updating the community.");
        }
    }

    [HttpDelete("{communityId}/members/{userId}")]
    [Authorize]
    public async Task<IActionResult> RemoveMember(int communityId, int userId)
    {
        try
        {
            var requesterId = GetUserId();
        
            // Sử dụng helper method để kiểm tra quyền Moderator
            if (!await IsUserModeratorAsync(communityId, requesterId))
            {
                // Nếu không phải Moderator, chỉ được phép tự rời nhóm
                if (requesterId != userId)
                {
                    return Forbid("Only Moderator can remove other members.");
                }
            
                // Kiểm tra xem người bị xóa có phải Moderator không
                if (await IsUserModeratorAsync(communityId, userId))
                {
                    return BadRequest("Moderator must transfer rights before leaving.");
                }
            }

            await _communityRepository.RemoveMemberAsync(communityId, userId, requesterId);
            return Ok(new { message = "Member removed successfully." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "RemoveMember failed for communityId: {CommunityId}, userId: {UserId}", 
                communityId, userId);
            return BadRequest(new { error = ex.Message });
        }
    }



    /// Helper method để lấy vai trò của user trong cộng đồng
  
    private async Task<string?> GetUserRoleAsync(int communityId, int userId)
    {
        var member = await _db.CommunityMembers.AsNoTracking()
            .FirstOrDefaultAsync(m => m.CommunityID == communityId && m.UserID == userId);
        return member?.Role.ToString();
    }


    /// Helper method kiểm tra xem user có phải là thành viên của cộng đồng không
  
    private async Task<bool> IsUserMemberAsync(int communityId, int userId)
    {
        return await _db.CommunityMembers.AnyAsync(m => m.CommunityID == communityId && m.UserID == userId);
    }


    /// Helper method kiểm tra xem user có phải là Moderator của cộng đồng không
    private int GetUserId() 
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
        {
            throw new UnauthorizedAccessException("User is not authenticated or invalid user ID.");
        }
        return userId;
    }

    private async Task<bool> IsUserModeratorAsync(int communityId, int userId)
    {
        try
        {
            return await _communityRepository.IsUserModeratorAsync(communityId, userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if user {UserId} is Moderator of community {CommunityId}", userId, communityId);
            return false;
        }
    }
}
