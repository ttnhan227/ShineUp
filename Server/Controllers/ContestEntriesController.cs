using Client.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Interfaces;
using Server.Models;
using Server.Repositories;

namespace Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ContestEntriesController : ControllerBase
    {
        private readonly IContestEntryRepositories _repository;
        private readonly DatabaseContext _context;

        public ContestEntriesController(IContestEntryRepositories repository, DatabaseContext context)
        {
            _repository = repository;
            _context = context;
        }

        [HttpPost]
        public async Task<IActionResult> Submit([FromBody] ContestEntryDTO dto)
        {
            var contest = await _context.Contests.FindAsync(dto.ContestID);
            if (contest == null || DateTime.UtcNow < contest.StartDate || DateTime.UtcNow > contest.EndDate)
                return BadRequest("This contest is not active.");

            if (await _repository.HasSubmittedAsync(dto.ContestID, dto.UserID))
                return BadRequest("You have already submitted.");

            var entity = new ContestEntry
            {
                ContestID = dto.ContestID,
                VideoID = dto.VideoID,
                UserID = dto.UserID,
                SubmissionDate = DateTime.UtcNow
            };
            await _repository.AddAsync(entity);
            dto.EntryID = entity.EntryID;
            dto.SubmissionDate = entity.SubmissionDate;
            return Ok(dto);
        }

        [HttpGet("contest/{contestId}")]
        public async Task<IActionResult> GetByContest(int contestId)
        {
            var entries = await _repository.GetEntriesByContestAsync(contestId);
            return Ok(entries);
        }

        [HttpGet("user/{userId}/contest/{contestId}")]
        public async Task<IActionResult> CheckUserSubmission(int userId, int contestId)
        {
            bool exists = await _repository.HasSubmittedAsync(contestId, userId);
            return Ok(new { hasSubmitted = exists });
        }
    }
}