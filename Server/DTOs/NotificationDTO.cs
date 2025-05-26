namespace Server.DTOs;

public class NotificationDTO
{
    public int NotificationID { get; set; }
    public int UserID { get; set; }
    public string NotificationType { get; set; }
    public string Message { get; set; }
    public Guid? VideoID { get; set; }
    public int? ContestID { get; set; }
    public int? MessageID { get; set; }
    public int? CommentID { get; set; }
    public int? TriggeredByUserID { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsRead { get; set; }
}