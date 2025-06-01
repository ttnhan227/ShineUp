namespace Server.DTOs;

public class NotificationCreateDTO
{
    public int UserID { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class NotificationDTO
{
    public int NotificationID { get; set; }
    public int UserID { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}