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
public class LikesController : ControllerBase
{
    private readonly ILikeRepository _likeRepository;
    private readonly INotificationRepository _notificationRepository;
    private readonly DatabaseContext _context;

    public LikesController(ILikeRepository likeRepository, INotificationRepository notificationRepository, DatabaseContext context)
    {
        _likeRepository = likeRepository;
        _notificationRepository = notificationRepository;
        _context = context;
    }

    [HttpPost]
    public async Task<IActionResult> Like([FromBody] LikeDTO dto)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null)
        {
            return Unauthorized("Invalid token");
        }

        var userId = int.Parse(userIdClaim.Value);
        var video = await _context.Videos.Include(v => v.User).FirstOrDefaultAsync(v => v.VideoID == Guid.Parse(dto.VideoID));
        if (video == null)
        {
            return NotFound("Video not found");
        }

        var existingLike = await _context.Likes
            .FirstOrDefaultAsync(l => l.VideoID == Guid.Parse(dto.VideoID) && l.UserID == userId);
        if (existingLike != null)
        {
            return BadRequest("You have already liked this video");
        }

        var like = new Like
        {
            VideoID = Guid.Parse(dto.VideoID),
            UserID = userId,
            CreatedAt = DateTime.UtcNow
        };

        await _likeRepository.AddAsync(like);

        // Create notification for video owner
        if (video.UserID != userId) // Don't notify if user likes their own video
        {
            var triggerUser = await _context.Users.FindAsync(userId);
            var notification = new Notification
            {
                UserID = video.UserID,
                NotificationType = "Like",
                Message = $"{triggerUser.Username} liked your video: {video.Title}",
                VideoID = video.VideoID,
                TriggeredByUserID = userId,
                CreatedAt = DateTime.UtcNow,
                IsRead = false
            };
            await _notificationRepository.AddAsync(notification);
        }

        dto.LikeID = like.LikeID;
        dto.UserID = like.UserID;
        dto.CreatedAt = like.CreatedAt;

        return Ok(dto);
    }
}