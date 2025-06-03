using Server.Models;

namespace Server.DTOs;

public class NotificationDTO
{
    public int NotificationID { get; set; }
    public int UserID { get; set; }
    public string Message { get; set; } = string.Empty;
    public NotificationType Type { get; set; }
    public NotificationStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public object? RelatedEntity { get; set; } // Can hold related opportunity or application
}

public class CreateNotificationDTO
{
    public int UserID { get; set; }
    public string Message { get; set; } = string.Empty;
    public NotificationType Type { get; set; } = NotificationType.Generic;
    public int? RelatedEntityId { get; set; }
    public string? RelatedEntityType { get; set; } // "Opportunity", "Application", etc.
}

public class UpdateNotificationDTO
{
    public NotificationStatus Status { get; set; } = NotificationStatus.Read;
}
