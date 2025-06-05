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
    private readonly ICommunityService _communityService;
    private readonly ILogger<CommunityController> _logger;
    private readonly DatabaseContext _db;

    public CommunityController(ICommunityService communityService, ILogger<CommunityController> logger,
        DatabaseContext db)
    {
        _communityService = communityService;
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
    public async Task<IActionResult> GetCommunityById(int communityId)
    {
        try
        {
            int userId = GetUserId(); // Lấy userId từ token
            if (communityId <= 0)
                return BadRequest("Invalid community ID.");
            if (userId <= 0)
                return BadRequest("Invalid user ID.");
            var community = await _communityService.GetCommunityDetailsAsync(communityId, userId);
            return Ok(community);
        }
        catch (KeyNotFoundException)
        {
            return NotFound("Community not found.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve community with ID {CommunityId}", communityId);
            return StatusCode(500, "Internal server error");
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
            var result = await _communityService.CreateCommunityAsync(dto, userId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create community");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetAllCommunities()
    {
        try
        {
            var result = await _communityService.GetAllCommunitiesAsync();
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve communities");
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
            await _communityService.JoinCommunityAsync(communityId, userId);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "JoinCommunity failed");
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("{communityId}/leave")]
    [Authorize]
    public async Task<IActionResult> LeaveCommunity(int communityId)
    {
        try
        {
            int userId = GetUserId();
            await _communityService.LeaveCommunityAsync(communityId, userId);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "LeaveCommunity failed");
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("{communityId}/transfer-admin")]
    [Authorize]
    public async Task<IActionResult> TransferAdmin(int communityId, [FromQuery] int newAdminId)
    {
        try
        {
            int currentAdminId = GetUserId();
            await _communityService.TransferAdminAsync(communityId, currentAdminId, newAdminId);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "TransferAdmin failed");
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("{communityId}/members")]
    public async Task<IActionResult> GetCommunityMembers(int communityId)
    {
        try
        {
            var members = await _communityService.GetCommunityMembersAsync(communityId);
            return Ok(members);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetCommunityMembers failed");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("{communityId}/posts")]
    public async Task<IActionResult> GetCommunityPosts(int communityId)
    {
        try
        {
            var posts = await _communityService.GetCommunityPostsAsync(communityId);
            return Ok(posts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetCommunityPosts failed");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpDelete("{communityId}/members/{userId}")]
    [Authorize]
    public async Task<IActionResult> RemoveMember(int communityId, int userId)
    {
        try
        {
            var requesterId = GetUserId();
        
            // Sử dụng helper method để kiểm tra quyền admin
            if (!await IsUserAdminAsync(communityId, requesterId))
            {
                // Nếu không phải admin, chỉ được phép tự rời nhóm
                if (requesterId != userId)
                {
                    return Forbid("Only admin can remove other members.");
                }
            
                // Kiểm tra xem người bị xóa có phải admin không
                if (await IsUserAdminAsync(communityId, userId))
                {
                    return BadRequest("Admin must transfer rights before leaving.");
                }
            }

            await _communityService.RemoveMemberAsync(communityId, userId, requesterId);
            return Ok(new { message = "Member removed successfully." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "RemoveMember failed for communityId: {CommunityId}, userId: {UserId}", 
                communityId, userId);
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPut("{communityId}")]
    [Authorize]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UpdateCommunity(int communityId, [FromForm] UpdateCommunityDTO dto)
    {
        try
        {
            int userId = GetUserId();
            bool isAdmin = await _communityService.IsUserAdminAsync(communityId, userId);
            if (!isAdmin)
                return Forbid("Only community admin can update community info.");

            var updated = await _communityService.UpdateCommunityAsync(communityId, dto, userId);
            return Ok(updated);
        }
        catch (KeyNotFoundException)
        {
            return NotFound("Community not found.");
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid("You do not have permission to update this community.");
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update community");
            return StatusCode(500, "Internal server error");
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


    /// Helper method kiểm tra xem user có phải là admin của cộng đồng không
    private async Task<bool> IsUserAdminAsync(int communityId, int userId)
    {
        var role = await GetUserRoleAsync(communityId, userId);
        return role == CommunityRole.Admin.ToString();
    }
    private int GetUserId() => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
}
