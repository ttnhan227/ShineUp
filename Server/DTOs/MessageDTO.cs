namespace Server.DTOs;

public class MessageDTO
{
    public int MessageID { get; set; }
    public int SenderID { get; set; }
    public int ReceiverID { get; set; }
    public string MessageContent { get; set; }
    public DateTime SentAt { get; set; }
}