using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.DTOs;
using Server.Interfaces;
using Server.Models;
using System.Security.Claims;

namespace Server.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class SharesController : ControllerBase
{
    private readonly IShareRepository _shareRepository;
    private readonly INotificationRepository _notificationRepository;
    private readonly DatabaseContext _context;

    public SharesController(IShareRepository shareRepository, INotificationRepository notificationRepository, DatabaseContext context)
    {
        _shareRepository = shareRepository;
        _notificationRepository = notificationRepository;
        _context = context;
    }

    [HttpPost]
    public async Task<IActionResult> Share([FromBody] ShareDTO dto)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null)
        {
            return Unauthorized("Invalid token");
        }

        var userId = int.Parse(userIdClaim.Value);
        if (await _shareRepository.HasSharedAsync(dto.VideoID, userId))
        {
            return BadRequest("You have already shared this video");
        }

        var video = await _context.Videos.Include(v => v.User).FirstOrDefaultAsync(v => v.VideoID == dto.VideoID);
        if (video == null)
        {
            return NotFound("Video not found");
        }

        var share = new Share
        {
            UserID = userId,
            VideoID = dto.VideoID,
            CreatedAt = DateTime.UtcNow,
            Platform = dto.Platform
        };

        await _shareRepository.AddAsync(share);

        // Create notification for video owner
        if (video.UserID != userId) // Don't notify if user shares their own video
        {
            var triggerUser = await _context.Users.FindAsync(userId);
            var notification = new Notification
            {
                UserID = video.UserID,
                NotificationType = "Share",
                Message = $"{triggerUser.Username} shared your video: {video.Title}",
                VideoID = video.VideoID,
                TriggeredByUserID = userId,
                CreatedAt = DateTime.UtcNow,
                IsRead = false
            };
            await _notificationRepository.AddAsync(notification);
        }

        dto.ShareID = share.ShareID;
        dto.UserID = share.UserID;
        dto.CreatedAt = share.CreatedAt;

        return Ok(dto);
    }
}