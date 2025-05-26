using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.DTOs;
using Server.Interfaces;
using Server.Models;

namespace Server.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ContestsController : ControllerBase
{
    private readonly IContestRepositories _contestRepository;
    private readonly INotificationRepository _notificationRepository;
    private readonly DatabaseContext _context;

    public ContestsController(IContestRepositories contestRepository, INotificationRepository notificationRepository, DatabaseContext context)
    {
        _contestRepository = contestRepository;
        _notificationRepository = notificationRepository;
        _context = context;
    }

    [HttpPost]
    public async Task<IActionResult> CreateContest([FromBody] ContestDTO dto)
    {
        var contest = new Contest
        {
            Title = dto.Title,
            Description = dto.Description,
            StartDate = dto.StartDate,
            EndDate = dto.EndDate
        };

        await _contestRepository.AddAsync(contest);

        // Notify all users about the new contest
        var users = await _context.Users.ToListAsync();
        foreach (var user in users)
        {
            var notification = new Notification
            {
                UserID = user.UserID,
                NotificationType = "ContestUpdate",
                Message = $"New contest created: {contest.Title}",
                ContestID = contest.ContestID,
                CreatedAt = DateTime.UtcNow,
                IsRead = false
            };
            await _notificationRepository.AddAsync(notification);
        }

        dto.ContestID = contest.ContestID;
        return Ok(dto);
    }

    [HttpPut("{contestId}")]
    public async Task<IActionResult> UpdateContest(int contestId, [FromBody] ContestDTO dto)
    {
        var contest = await _contestRepository.GetByIdAsync(contestId);
        if (contest == null)
        {
            return NotFound("Contest not found");
        }

        contest.Title = dto.Title;
        contest.Description = dto.Description;
        contest.StartDate = dto.StartDate;
        contest.EndDate = dto.EndDate;

        await _contestRepository.UpdateAsync(contest);

        // Notify all users about the contest update
        var users = await _context.Users.ToListAsync();
        foreach (var user in users)
        {
            var notification = new Notification
            {
                UserID = user.UserID,
                NotificationType = "ContestUpdate",
                Message = $"Contest updated: {contest.Title}",
                ContestID = contest.ContestID,
                CreatedAt = DateTime.UtcNow,
                IsRead = false
            };
            await _notificationRepository.AddAsync(notification);
        }

        return Ok(dto);
    }
}