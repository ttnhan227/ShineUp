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
public class CommentsController : ControllerBase
{
    private readonly ICommentRepository _commentRepository;
    private readonly INotificationRepository _notificationRepository;
    private readonly DatabaseContext _context;

    public CommentsController(ICommentRepository commentRepository, INotificationRepository notificationRepository, DatabaseContext context)
    {
        _commentRepository = commentRepository;
        _notificationRepository = notificationRepository;
        _context = context;
    }

    [HttpGet("video/{videoId}")]
    [AllowAnonymous] // Allow public access to view comments
    public async Task<IActionResult> GetComments(Guid videoId)
    {
        var comments = await _context.Comments
            .Include(c => c.User)
            .Where(c => c.VideoID == videoId)
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => new CommentDTO
            {
                CommentID = c.CommentID,
                VideoID = c.VideoID.ToString(),
                UserID = c.UserID,
                Content = c.Content,
                CreatedAt = c.CreatedAt
            })
            .ToListAsync();

        return Ok(comments);
    }

    [HttpPost]
    public async Task<IActionResult> PostComment([FromBody] CommentDTO dto)
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

        var comment = new Comment
        {
            VideoID = Guid.Parse(dto.VideoID),
            UserID = userId,
            Content = dto.Content,
            CreatedAt = DateTime.UtcNow
        };

        await _commentRepository.AddAsync(comment);

        // Create notification for video owner
        if (video.UserID != userId) // Don't notify if user comments on their own video
        {
            var triggerUser = await _context.Users.FindAsync(userId);
            var notification = new Notification
            {
                UserID = video.UserID,
                NotificationType = "Comment",
                Message = $"{triggerUser.Username} commented on your video: {video.Title}",
                VideoID = video.VideoID,
                CommentID = comment.CommentID,
                TriggeredByUserID = userId,
                CreatedAt = DateTime.UtcNow,
                IsRead = false
            };
            await _notificationRepository.AddAsync(notification);
        }

        dto.CommentID = comment.CommentID;
        dto.UserID = comment.UserID;
        dto.CreatedAt = comment.CreatedAt;

        return Ok(dto);
    }
}