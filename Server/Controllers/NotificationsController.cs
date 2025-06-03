using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Server.DTOs;
using Server.Interfaces;

namespace Server.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly INotificationRepository _notificationRepository;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public NotificationsController(
        INotificationRepository notificationRepository,
        IHttpContextAccessor httpContextAccessor)
    {
        _notificationRepository = notificationRepository;
        _httpContextAccessor = httpContextAccessor;
    }

    private int GetCurrentUserId()
{
    var userIdClaim = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                    ?? _httpContextAccessor.HttpContext?.User.FindFirst("id")?.Value
                    ?? throw new UnauthorizedAccessException("User ID claim not found");
    
    if (!int.TryParse(userIdClaim, out int userId))
    {
        throw new UnauthorizedAccessException("Invalid user ID format");
    }
    
    return userId;
}

    // GET: api/notifications
    [HttpGet]
    public async Task<ActionResult<IEnumerable<NotificationDTO>>> GetNotifications([FromQuery] bool unreadOnly = false)
    {
        var userId = GetCurrentUserId();
        var notifications = await _notificationRepository.GetUserNotificationsAsync(userId, unreadOnly);
        return Ok(notifications);
    }

    // GET: api/notifications/5
    [HttpGet("{id}")]
    public async Task<ActionResult<NotificationDTO>> GetNotification(int id)
    {
        var userId = GetCurrentUserId();
        var notification = await _notificationRepository.GetByIdAsync(id, userId);
        
        if (notification == null)
        {
            return NotFound();
        }
        
        return Ok(notification);
    }

    // POST: api/notifications
    [HttpPost]
    [Authorize(Roles = "Admin")] // Only admins can create notifications directly
    public async Task<ActionResult<NotificationDTO>> CreateNotification(CreateNotificationDTO notificationDto)
    {
        var notification = await _notificationRepository.CreateNotificationAsync(notificationDto);
        return CreatedAtAction(nameof(GetNotification), new { id = notification.NotificationID }, notification);
    }

    // PUT: api/notifications/5/read
    [HttpPut("{id}/read")]
    public async Task<IActionResult> MarkAsRead(int id)
    {
        var userId = GetCurrentUserId();
        var success = await _notificationRepository.MarkAsReadAsync(id, userId);
        
        if (!success)
        {
            return NotFound();
        }
        
        return NoContent();
    }

    // PUT: api/notifications/read-all
    [HttpPut("read-all")]
    public async Task<IActionResult> MarkAllAsRead()
    {
        var userId = GetCurrentUserId();
        await _notificationRepository.MarkAllAsReadAsync(userId);
        return NoContent();
    }

    // DELETE: api/notifications/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteNotification(int id)
    {
        var userId = GetCurrentUserId();
        var success = await _notificationRepository.DeleteNotificationAsync(id, userId);
        
        if (!success)
        {
            return NotFound();
        }
        
        return NoContent();
    }

    // GET: api/notifications/unread-count
    [HttpGet("unread-count")]
    public async Task<ActionResult<int>> GetUnreadCount()
    {
        var userId = GetCurrentUserId();
        var count = await _notificationRepository.GetUnreadCountAsync(userId);
        return Ok(count);
    }
}
