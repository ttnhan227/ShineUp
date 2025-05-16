using Microsoft.AspNetCore.Mvc;
using Client.DTOs;
using Server.Models;
using Server.Repositories;
using Server.Interfaces;

namespace Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ContestEntriesController : ControllerBase
    {
        private readonly IContestEntryRepository _repository;

        public ContestEntriesController(IContestEntryRepository repository)
        {
            _repository = repository;
        }

        [HttpPost]
        public async Task<IActionResult> Submit([FromBody] ContestEntryDTO dto)
        {
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
