using Client.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Interfaces;
using Server.Models;
using Server.Repositories;
using System.Text;

namespace Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class VotesController : ControllerBase
    {
        private readonly IVoteRepositories _repository;
        private readonly DatabaseContext _context;

        public VotesController(IVoteRepositories repository, DatabaseContext context)
        {
            _repository = repository;
            _context = context;
        }

        [HttpPost]
        public async Task<IActionResult> Vote([FromBody] VoteDTO dto)
        {
            var entry = await _context.ContestEntries.Include(e => e.Contest)
                                                     .FirstOrDefaultAsync(e => e.EntryID == dto.EntryID);
            if (entry == null || DateTime.UtcNow < entry.Contest.StartDate || DateTime.UtcNow > entry.Contest.EndDate)
                return BadRequest("Voting is not allowed at this time.");

            if (await _repository.HasVotedAsync(dto.EntryID, dto.UserID))
                return BadRequest("Already voted.");

            var vote = new Vote
            {
                EntryID = dto.EntryID,
                UserID = dto.UserID,
                VotedAt = DateTime.UtcNow
            };

            await _repository.AddAsync(vote);
            dto.VoteID = vote.VoteID;
            dto.VotedAt = vote.VotedAt;
            return Ok(dto);
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
                    VideoURL = g.First().ContestEntry.Video.VideoURL,
                    UserName = g.First().ContestEntry.User.Username
                })
                .ToListAsync();

            return Ok(results);
        }
    }
}
