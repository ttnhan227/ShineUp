namespace Client.Models;

public class NotificationViewModel
{
    public int NotificationID { get; set; }
    public int UserID { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string CreatedAt { get; set; } = string.Empty; // Formatted date string
}