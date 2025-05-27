namespace Server.Models;

public class Conversation
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public bool IsGroup { get; set; } = false; // Indicates if the conversation is a group chat or a private chat
    public string? GroupName { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<UserConversation> Participants { get; set; }
    public ICollection<Message> Messages { get; set; }
}