using Microsoft.AspNetCore.Mvc;
using Server.Data;
using Server.DTOs;
using Server.Interfaces;
using Server.Models;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OpportunitiesController : ControllerBase
{
    private readonly IOpportunityApplicationRepository _applicationRepository;
    private readonly INotificationRepository _notificationRepository;
    private readonly DatabaseContext _context;

    public OpportunitiesController(
        IOpportunityApplicationRepository applicationRepository,
        INotificationRepository notificationRepository,
        DatabaseContext context)
    {
        _applicationRepository = applicationRepository;
        _notificationRepository = notificationRepository;
        _context = context;
    }

    [HttpPost("applications")]
    public async Task<IActionResult> CreateApplication([FromBody] OpportunityApplicationCreateDTO dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var user = await _context.Users.FindAsync(dto.UserID);
        if (user == null)
            return BadRequest(new { message = "User not found" });

        var application = new OpportunityApplication
        {
            UserID = dto.UserID,
            OpportunityTitle = dto.OpportunityTitle,
            OpportunityDescription = dto.OpportunityDescription,
            AppliedAt = DateTime.UtcNow
        };

        var createdApplication = await _applicationRepository.CreateAsync(application);

        var response = new OpportunityApplicationDTO
        {
            ApplicationID = createdApplication.ApplicationID,
            UserID = createdApplication.UserID,
            Username = user.Username,
            OpportunityTitle = createdApplication.OpportunityTitle,
            OpportunityDescription = createdApplication.OpportunityDescription,
            AppliedAt = createdApplication.AppliedAt
        };

        return Ok(response);
    }

    [HttpGet("applications/user/{userId}")]
    public async Task<IActionResult> GetUserApplications(int userId)
    {
        var applications = await _applicationRepository.GetByUserIdAsync(userId);
        var response = applications.Select(a => new OpportunityApplicationDTO
        {
            ApplicationID = a.ApplicationID,
            UserID = a.UserID,
            Username = a.User.Username,
            OpportunityTitle = a.OpportunityTitle,
            OpportunityDescription = a.OpportunityDescription,
            AppliedAt = a.AppliedAt
        }).ToList();

        return Ok(response);
    }

    [HttpPost("notifications")]
    public async Task<IActionResult> CreateNotification([FromBody] NotificationCreateDTO dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var user = await _context.Users.FindAsync(dto.UserID);
        if (user == null)
            return BadRequest(new { message = "User not found" });

        var notification = new Notification
        {
            UserID = dto.UserID,
            Message = dto.Message,
            CreatedAt = DateTime.UtcNow
        };

        var createdNotification = await _notificationRepository.CreateAsync(notification);

        var response = new NotificationDTO
        {
            NotificationID = createdNotification.NotificationID,
            UserID = createdNotification.UserID,
            Username = user.Username,
            Message = createdNotification.Message,
            CreatedAt = createdNotification.CreatedAt
        };

        return Ok(response);
    }

    [HttpGet("notifications/user/{userId}")]
    public async Task<IActionResult> GetUserNotifications(int userId)
    {
        var notifications = await _notificationRepository.GetByUserIdAsync(userId);
        var response = notifications.Select(n => new NotificationDTO
        {
            NotificationID = n.NotificationID,
            UserID = n.UserID,
            Username = n.User.Username,
            Message = n.Message,
            CreatedAt = n.CreatedAt
        }).ToList();

        return Ok(response);
    }

    [HttpGet("applications/{id}")]
    public async Task<IActionResult> GetApplication(int id)
    {
        var application = await _applicationRepository.GetByIdAsync(id);
        if (application == null)
            return NotFound();

        var response = new OpportunityApplicationDTO
        {
            ApplicationID = application.ApplicationID,
            UserID = application.UserID,
            Username = application.User.Username,
            OpportunityTitle = application.OpportunityTitle,
            OpportunityDescription = application.OpportunityDescription,
            AppliedAt = application.AppliedAt
        };

        return Ok(response);
    }

    [HttpPut("applications/{id}")]
    public async Task<IActionResult> UpdateApplication(int id, [FromBody] OpportunityApplicationDTO dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var existingApplication = await _applicationRepository.GetByIdAsync(id);
        if (existingApplication == null)
            return NotFound();

        // Verify the user owns this application
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                          ?? User.FindFirst("UserID")?.Value 
                          ?? User.FindFirst("sub")?.Value;

        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId) || 
            existingApplication.UserID != userId)
        {
            return Unauthorized();
        }

        // Update the application
        existingApplication.OpportunityTitle = dto.OpportunityTitle;
        existingApplication.OpportunityDescription = dto.OpportunityDescription;

        await _context.SaveChangesAsync();

        var response = new OpportunityApplicationDTO
        {
            ApplicationID = existingApplication.ApplicationID,
            UserID = existingApplication.UserID,
            Username = existingApplication.User.Username,
            OpportunityTitle = existingApplication.OpportunityTitle,
            OpportunityDescription = existingApplication.OpportunityDescription,
            AppliedAt = existingApplication.AppliedAt
        };

        return Ok(response);
    }

    [HttpDelete("applications/{id}")]
    public async Task<IActionResult> DeleteApplication(int id)
    {
        var application = await _applicationRepository.GetByIdAsync(id);
        if (application == null)
            return NotFound();

        // Verify the user owns this application
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                          ?? User.FindFirst("UserID")?.Value 
                          ?? User.FindFirst("sub")?.Value;

        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId) || 
            application.UserID != userId)
        {
            return Unauthorized();
        }


        var result = await _applicationRepository.DeleteAsync(id);
        if (!result)
            return StatusCode(500, "An error occurred while deleting the application.");

        return NoContent();
    }
}