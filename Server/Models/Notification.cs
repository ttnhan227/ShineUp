namespace Server.Models;

public class Notification
{
    public int NotificationID { get; set; }
    public int UserID { get; set; }
    public User User { get; set; } // Link to user receiving notification
    public string Message { get; set; } = string.Empty; // e.g., "New Art gig available!"
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}