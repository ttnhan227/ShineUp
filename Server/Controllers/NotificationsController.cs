using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.DTOs;
using Server.Interfaces;
using System.Security.Claims;

namespace Server.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class NotificationsController : ControllerBase
{
    private readonly INotificationRepository _notificationRepository;
    private readonly DatabaseContext _context;

    public NotificationsController(INotificationRepository notificationRepository, DatabaseContext context)
    {
        _notificationRepository = notificationRepository;
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetNotifications([FromQuery] bool includeRead = true)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null)
        {
            return Unauthorized("Invalid token");
        }

        var userId = int.Parse(userIdClaim.Value);
        var notifications = await _notificationRepository.GetByUserIdAsync(userId, includeRead);

        var notificationDtos = notifications.Select(n => new NotificationDTO
        {
            NotificationID = n.NotificationID,
            UserID = n.UserID,
            NotificationType = n.NotificationType,
            Message = n.Message,
            VideoID = n.VideoID,
            ContestID = n.ContestID,
            MessageID = n.MessageID,
            CommentID = n.CommentID,
            TriggeredByUserID = n.TriggeredByUserID,
            CreatedAt = n.CreatedAt,
            IsRead = n.IsRead
        }).ToList();

        return Ok(notificationDtos);
    }

    [HttpPut("{notificationId}/read")]
    public async Task<IActionResult> MarkAsRead(int notificationId)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null)
        {
            return Unauthorized("Invalid token");
        }

        var userId = int.Parse(userIdClaim.Value);
        var notification = await _context.Notifications.FirstOrDefaultAsync(n => n.NotificationID == notificationId && n.UserID == userId);
        if (notification == null)
        {
            return NotFound("Notification not found or you don't have access");
        }

        await _notificationRepository.MarkAsReadAsync(notificationId);
        return Ok(new { Message = "Notification marked as read" });
    }
}