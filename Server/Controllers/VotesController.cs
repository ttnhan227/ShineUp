using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.DTOs;
using Server.Interfaces;
using Server.Models;
using System.Security.Claims;

namespace Server.Controllers;

// Bỏ phiếu và xem kết quả
[ApiController]
[Route("api/[controller]")]
public class VotesController : ControllerBase
{
    // 1. Constructor
    private readonly IVoteRepository _repo;

    public VotesController(IVoteRepository repo)
    {
        _repo = repo;
    }

    // 2. [HttpPost] Vote() – Tiến hành vote cho một entry
    [HttpPost]
    public async Task<IActionResult> Vote([FromBody] CastVoteDTO dto)
    {
        var userId = GetUserIdFromClaims();
        if (await _repo.HasUserVotedAsync(dto.EntryID, userId))
            return BadRequest("User đã vote cho entry này");

        var vote = new Vote
        {
            EntryID = dto.EntryID,
            UserID = userId,
            VotedAt = DateTime.UtcNow
        };

        var created = await _repo.CastVoteAsync(vote);
        return Ok(created);
    }

    // 3. [HttpGet("hasvoted/{entryId}")] – Kiểm tra người dùng đã vote cho entry này chưa
    [HttpGet("results/{contestId}")]
    public async Task<IActionResult> Results(int contestId)
        => Ok(await _repo.GetVoteResultsByContestAsync(contestId));

    // 4. Lấy ID người dùng từ token trong Claims (token chứa ClaimTypes.NameIdentifier là UserID)
    private int GetUserIdFromClaims()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return userIdClaim != null ? int.Parse(userIdClaim) : throw new UnauthorizedAccessException();
    }
}