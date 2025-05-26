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
public class MessagesController : ControllerBase
{
    private readonly IMessageRepository _messageRepository;
    private readonly INotificationRepository _notificationRepository;
    private readonly DatabaseContext _context;

    public MessagesController(IMessageRepository messageRepository, INotificationRepository notificationRepository, DatabaseContext context)
    {
        _messageRepository = messageRepository;
        _notificationRepository = notificationRepository;
        _context = context;
    }

    [HttpPost]
    public async Task<IActionResult> SendMessage([FromBody] MessageDTO dto)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null)
        {
            return Unauthorized("Invalid token");
        }

        var senderId = int.Parse(userIdClaim.Value);
        var receiver = await _context.Users.FindAsync(dto.ReceiverID);
        if (receiver == null)
        {
            return NotFound("Receiver not found");
        }

        var message = new Message
        {
            SenderID = senderId,
            ReceiverID = dto.ReceiverID,
            MessageContent = dto.MessageContent,
            SentAt = DateTime.UtcNow
        };

        await _messageRepository.AddAsync(message);

        // Create notification for receiver
        var sender = await _context.Users.FindAsync(senderId);
        var notification = new Notification
        {
            UserID = dto.ReceiverID,
            NotificationType = "Message",
            Message = $"{sender.Username} sent you a message",
            MessageID = message.MessageID,
            TriggeredByUserID = senderId,
            CreatedAt = DateTime.UtcNow,
            IsRead = false
        };
        await _notificationRepository.AddAsync(notification);

        dto.MessageID = message.MessageID;
        dto.SenderID = message.SenderID;
        dto.SentAt = message.SentAt;

        return Ok(dto);
    }

    [HttpGet("user/{userId}")]
    public async Task<IActionResult> GetMessages(int userId)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null)
        {
            return Unauthorized("Invalid token");
        }

        var currentUserId = int.Parse(userIdClaim.Value);
        if (userId != currentUserId)
        {
            return Unauthorized("You can only view your own messages");
        }

        var messages = await _context.Messages
            .Where(m => m.SenderID == userId || m.ReceiverID == userId)
            .OrderByDescending(m => m.SentAt)
            .Select(m => new MessageDTO
            {
                MessageID = m.MessageID,
                SenderID = m.SenderID,
                ReceiverID = m.ReceiverID,
                MessageContent = m.MessageContent,
                SentAt = m.SentAt
            })
            .ToListAsync();

        return Ok(messages);
    }
}