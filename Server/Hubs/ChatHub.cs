using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Models;

namespace Server.Hubs;

public class ChatHub : Hub
{
    private readonly DatabaseContext _db;

    public ChatHub(DatabaseContext db)
    {
        _db = db;
    }

    public async Task JoinConversation(string conversationId)
    {
        var userId = int.Parse(Context.UserIdentifier ?? "0");

        var isMember = await _db.UserConversations
            .AnyAsync(x => x.ConversationId == conversationId && x.UserId == userId);

        if (!isMember)
            throw new HubException("You are not a member of this conversation.");

        await Groups.AddToGroupAsync(Context.ConnectionId, conversationId);
    }

    public async Task SendMessage(string conversationId, string content)
    {
        var senderId = int.Parse(Context.UserIdentifier ?? "0");

        var isMember = await _db.UserConversations
            .AnyAsync(x => x.ConversationId == conversationId && x.UserId == senderId);
        if (!isMember)
            throw new HubException("You are not a member of this conversation.");

        var msg = new Message
        {
            Id = Guid.NewGuid().ToString(),
            ConversationId = conversationId,
            SenderId = senderId,
            Content = content,
            SentAt = DateTime.UtcNow,
            Type = MessageType.Text
        };

        _db.Messages.Add(msg);
        await _db.SaveChangesAsync();

        await Clients.Group(conversationId).SendAsync("ReceiveMessage", new
        {
            msg.Id,
            msg.ConversationId,
            msg.SenderId,
            msg.Content,
            msg.SentAt
        });
    }
}