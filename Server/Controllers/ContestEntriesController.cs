using Microsoft.AspNetCore.Mvc;
using Server.Data;
using Server.DTOs;
using Server.Interfaces;
using Server.Models;

namespace Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ContestEntriesController : ControllerBase
{
    private readonly DatabaseContext _context;
    private readonly IContestEntryRepositories _repository;

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
        {
            return BadRequest("This contest is not active.");
        }

        if (await _repository.HasSubmittedAsync(dto.ContestID, dto.UserID))
        {
            return BadRequest("You have already submitted.");
        }

        var entity = new ContestEntry
        {
            ContestID = dto.ContestID,
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
        var exists = await _repository.HasSubmittedAsync(contestId, userId);
        return Ok(new { hasSubmitted = exists });
    }
}