using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Server.DTOs;
using Server.Interfaces;
using System.Text.Json;

namespace Server.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly INotificationRepository _notificationRepository;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<NotificationsController> _logger;

    public NotificationsController(
        INotificationRepository notificationRepository,
        IHttpContextAccessor httpContextAccessor,
        ILogger<NotificationsController> logger)
    {
        _notificationRepository = notificationRepository;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    private int GetCurrentUserId()
    {
        if (!User.Identity.IsAuthenticated)
        {
            _logger.LogWarning("User is not authenticated");
            throw new UnauthorizedAccessException("User is not authenticated");
        }

        // Log all claims for debugging
        var claims = User.Claims.Select(c => $"{c.Type}: {c.Value}");
        _logger.LogInformation($"User claims: {string.Join(", ", claims)}");

        // Try different claim types
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                           ?? User.FindFirst("sub")?.Value
                           ?? User.FindFirst("id")?.Value
                           ?? User.FindFirst("userid")?.Value
                           ?? User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value;

        if (string.IsNullOrEmpty(userIdClaim))
        {
            _logger.LogError("No valid user ID claim found in token");
            throw new UnauthorizedAccessException("No valid user ID claim found in token");
        }

        if (!int.TryParse(userIdClaim, out int userId))
        {
            _logger.LogError($"Invalid user ID format in token: {userIdClaim}");
            throw new UnauthorizedAccessException("Invalid user ID format in token");
        }

        _logger.LogInformation($"Successfully extracted user ID: {userId}");
        return userId;
    }

    // GET: api/notifications
    [HttpGet]
    public async Task<ActionResult<IEnumerable<NotificationDTO>>> GetNotifications([FromQuery] bool unreadOnly = false)
    {
        try
        {
            _logger.LogInformation($"[NotificationsController] Starting GetNotifications. User authenticated: {User.Identity.IsAuthenticated}");
            
            var userId = GetCurrentUserId();
            _logger.LogInformation($"[NotificationsController] Getting notifications for user ID: {userId}, unreadOnly: {unreadOnly}");
            
            var notifications = await _notificationRepository.GetUserNotificationsAsync(userId, unreadOnly);
            _logger.LogInformation($"[NotificationsController] Found {notifications.Count()} notifications for user {userId}");

            // Log the first few notifications for debugging
            foreach (var notification in notifications.Take(3))
            {
                _logger.LogInformation($"[NotificationsController] Notification {notification.NotificationID} - Type: {notification.Type}, Status: {notification.Status}, Message: {notification.Message}");
            }
            
            return Ok(notifications);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"[NotificationsController] Error in GetNotifications: {ex.Message}");
            return StatusCode(500, new { message = "An error occurred while retrieving notifications" });
        }
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

    [HttpPost]
[Authorize(Roles = "Admin")]
public async Task<ActionResult<NotificationDTO>> CreateNotification(CreateNotificationDTO notificationDto)
{
    // Add this logging
    Console.WriteLine("=== Incoming Request ===");
    Console.WriteLine($"Raw notificationDto: {JsonSerializer.Serialize(notificationDto)}");
    Console.WriteLine($"UserID: {notificationDto?.UserID}");
    Console.WriteLine("=======================");
    
    // Add logging
    Console.WriteLine("User claims:");
    foreach (var claim in User.Claims)
    {
        Console.WriteLine($"{claim.Type}: {claim.Value}");
    }
    
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
