using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Interfaces;
using Server.Models;
using System.Security.Claims;

namespace Server.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class VotesController : ControllerBase
{
    private readonly DatabaseContext _context;
    private readonly ILogger<VotesController> _logger;
    private readonly IVoteRepositories _repository;

    public VotesController(IVoteRepositories repository, DatabaseContext context,
        ILogger<VotesController> logger = null)
    {
        _repository = repository;
        _context = context;
        _logger = logger;
    }

    [HttpPost]
    [Produces("application/json")]
    [Consumes("application/json")]
    [Authorize]
    public async Task<IActionResult> Vote([FromBody] VoteRequest request)
    {
        var logger = _logger ?? HttpContext.RequestServices.GetRequiredService<ILogger<VotesController>>();

        try
        {
            logger.LogInformation("Vote request received for entry {EntryId}", request?.EntryId);

            if (request == null || request.EntryId <= 0)
            {
                logger.LogWarning("Invalid vote request");
                return BadRequest(new { message = "Invalid request" });
            }

            if (!User.Identity.IsAuthenticated)
            {
                return Unauthorized(new { message = "Please sign in to vote" });
            }

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            {
                logger.LogWarning("Invalid user ID in token");
                return Unauthorized(new { message = "Invalid user identity" });
            }

            logger.LogInformation("Authenticated user ID: {UserId}", userId);

            var entry = await _context.ContestEntries
                .Include(e => e.Contest)
                .FirstOrDefaultAsync(e => e.EntryID == request.EntryId);

            if (entry == null)
            {
                logger.LogWarning("Entry not found: {EntryId}", request.EntryId);
                return NotFound(new { message = "Entry not found" });
            }

            // Check if the contest is active
            if (DateTime.UtcNow < entry.Contest.StartDate || DateTime.UtcNow > entry.Contest.EndDate)
            {
                return BadRequest(new { message = "Voting is only allowed during the contest period" });
            }

            // Check if user has already voted for this entry
            var existingVote = await _context.Votes
                .FirstOrDefaultAsync(v => v.EntryID == request.EntryId && v.UserID == userId);

            if (existingVote != null)
            {
                // User already voted, remove the vote
                _context.Votes.Remove(existingVote);
                await _context.SaveChangesAsync();

                // Get updated vote count
                var voteCount = await _context.Votes.CountAsync(v => v.EntryID == request.EntryId);

                return Ok(new
                {
                    message = "Vote removed successfully",
                    voteCount,
                    hasVoted = false
                });
            }
            else
            {
                // Add new vote
                var vote = new Vote
                {
                    EntryID = request.EntryId,
                    UserID = userId,
                    VotedAt = DateTime.UtcNow
                };

                _context.Votes.Add(vote);
                await _context.SaveChangesAsync();

                // Get updated vote count
                var voteCount = await _context.Votes.CountAsync(v => v.EntryID == request.EntryId);

                return Ok(new
                {
                    message = "Vote recorded successfully",
                    voteCount,
                    hasVoted = true
                });
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing vote for entry {EntryId}", request?.EntryId);
            return StatusCode(500, new { message = "An error occurred while processing your vote." });
        }
    }

    [HttpGet("contest/{contestId}")]
    public async Task<IActionResult> Results(int contestId)
    {
        var results = await _context.Votes
            .Include(v => v.ContestEntry)
            .ThenInclude(e => e.Video)
            .Include(v => v.ContestEntry.User)
            .Where(v => v.ContestEntry.ContestID == contestId)
            .GroupBy(v => v.EntryID)
            .Select(g => new
            {
                EntryID = g.Key,
                VoteCount = g.Count(),
                VideoTitle = g.First().ContestEntry.Video.Title,
                g.First().ContestEntry.Video.VideoURL,
                UserName = g.First().ContestEntry.User.Username
            })
            .ToListAsync();

        return Ok(results);
    }

    public class VoteRequest
    {
        public int EntryId { get; set; }
    }
}