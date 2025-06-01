using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Client.Models;

public class Message
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string ConversationId { get; set; }
    public int SenderId { get; set; } // User ID of the sender

    public string Content { get; set; } = string.Empty;
    public MessageType Type { get; set; } = MessageType.Text;

    public DateTime SentAt { get; set; } = DateTime.UtcNow;
    public bool IsRead { get; set; } = false;

    public string? MediaUrl { get; set; }  // if Type is Image, Video, or File, this will contain the URL to the media file
    
    public User Sender { get; set; }     //  1-n with User
    public Conversation Conversation { get; set; } // 1-n with Conversation
}

public enum MessageType // Type of message content
{
    Text,
    Image,
    Video,
    File,
    System
}