namespace Client.Models;

public class UserConversation // Represents the relationship between a user and a conversation (many-to-many relationship)
{
    public int UserId { get; set; }
    public string ConversationId { get; set; }

    public User User { get; set; }
    public Conversation Conversation { get; set; }
}