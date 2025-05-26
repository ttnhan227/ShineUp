using Server.Models;

namespace Server.DTOs;

public class SendMessageDTO
{
    public string ConversationId { get; set; }
    public string Content { get; set; }
    public MessageType Type { get; set; } = MessageType.Text;
}