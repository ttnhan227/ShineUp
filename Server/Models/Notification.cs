namespace Server.Models;

public enum NotificationType
{
    Generic,        // General purpose notification
    OpportunityPosted,
    ApplicationUpdate,
    MessageReceived,
    TalentMatch,
    SystemAlert
}

public enum NotificationStatus
{
    Unread,
    Read,
    Archived
}

public class Notification
{
    public int NotificationID { get; set; }
    public int UserID { get; set; }
    public User User { get; set; } // Link to user receiving notification
    public string Message { get; set; } = string.Empty;
    public NotificationType Type { get; set; } = NotificationType.Generic;
    public NotificationStatus Status { get; set; } = NotificationStatus.Unread;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}