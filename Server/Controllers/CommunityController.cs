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
        DatabaseContext _db)
    {
        _communityService = communityService;
        _logger = logger;
        this._db = _db;
    }


    [HttpGet("privacy-options")]
    public async Task<ActionResult<IEnumerable<Privacy>>> GetAllowedPrivacyOptions()
    {
        return await _db.Privacies
            .Where(p => p.PrivacyID == 1 || p.PrivacyID == 3)
            .ToListAsync();
    }



    [HttpPost]
    [Authorize]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> CreateCommunity([FromForm] CreateCommunityDTO dto)
    {
        try
        {
            int userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
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
            int userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
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
            int userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
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
            int currentAdminId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
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

    [Authorize]
    [HttpDelete("{communityId}/members/{userId}")]
    public async Task<IActionResult> RemoveMember(int communityId, int userId)
    {
        try
        {
            var requesterId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
            await _communityService.RemoveMemberAsync(communityId, userId, requesterId);
            return Ok(new { message = "Member removed successfully." });
        }
        catch (Exception ex)
        {
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
            int userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);

            // Kiểm tra quyền admin trước khi cho phép update
            bool isAdmin = await _communityService.IsUserAdminAsync(communityId, userId);
            if (!isAdmin)
            {
                return Forbid("Only community admin can update community info.");
            }

            var updatedCommunity = await _communityService.UpdateCommunityAsync(communityId, dto, userId);
            return Ok(updatedCommunity);
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

}