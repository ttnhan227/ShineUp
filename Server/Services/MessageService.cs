using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.DTOs;
using Server.Hubs;
using Server.Interfaces;
using Server.Models;


namespace Server.Services;



public class MessageService : IMessageService
{
    private readonly DatabaseContext _db;
    private readonly IHubContext<ChatHub> _hub;
    private readonly ILogger<MessageService> _logger;

    public MessageService(DatabaseContext db, IHubContext<ChatHub> hub, ILogger<MessageService> logger)
    {
        _db = db;
        _hub = hub;
        _logger = logger;
    }

    public async Task<Message> SendMessageAsync(int senderId, SendMessageDTO dto)
    {
        // Validate content
        if (string.IsNullOrWhiteSpace(dto.ConversationId) || string.IsNullOrWhiteSpace(dto.Content))
            throw new ArgumentException("Conversation ID and content cannot be empty.");    

        if (dto.Content.Length > 1000)
            throw new ArgumentException("Content exceeds maximum length of 1000 characters.");

        var isMember = await _db.UserConversations
            .AnyAsync(x => x.ConversationId == dto.ConversationId && x.UserId == senderId);

        if (!isMember)
            throw new UnauthorizedAccessException("You are not a member of this conversation.");

        var msg = new Message
        {
            Id = Guid.NewGuid().ToString(),
            ConversationId = dto.ConversationId,
            SenderId = senderId,
            Content = dto.Content.Trim(),
            Type = dto.Type,
            SentAt = DateTime.UtcNow
        };

        _db.Messages.Add(msg);
        await _db.SaveChangesAsync();

        // Push realtime message via SignalR for all participants in the conversation (it could be a group chat or a private chat)
        await _hub.Clients.Group(dto.ConversationId).SendAsync("ReceiveMessage", new
        {
            msg.Id,
            msg.ConversationId,
            msg.SenderId,
            msg.Content,
            msg.Type,
            msg.SentAt
        });
        
        _logger.LogInformation("Message sent: {ConversationId} by {UserId} at {Time}", msg.ConversationId, msg.SenderId, msg.SentAt);

        return msg;
    }
}