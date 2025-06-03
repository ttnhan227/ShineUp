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
    public string Message { get; set; } = string.Empty; // e.g., "New Art gig available!"
    public string? Title { get; set; }
    public NotificationType Type { get; set; } = NotificationType.Generic;
    public NotificationStatus Status { get; set; } = NotificationStatus.Unread;
    public bool IsActionRequired { get; set; } = false;
    public string? ActionLink { get; set; } // URL to relevant page
    public int? RelatedEntityID { get; set; } // ID of related entity (e.g., opportunity ID)
    public string? RelatedEntityType { get; set; } // Type of related entity (e.g., "Opportunity", "Application")
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ReadAt { get; set; }
}