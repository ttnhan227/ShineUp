using System;

namespace Client.Models
{
    public class NotificationViewModel
    {
        public int NotificationID { get; set; }
        public int UserID { get; set; }
        public string Message { get; set; } = string.Empty;
        public NotificationType Type { get; set; }
        public NotificationStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public object? RelatedEntity { get; set; }
    }

    public class CreateNotificationViewModel
    {
        public int UserID { get; set; }
        public string Message { get; set; } = string.Empty;
        public NotificationType Type { get; set; } = NotificationType.Generic;
        public int? RelatedEntityId { get; set; }
        public string? RelatedEntityType { get; set; }
    }

    public class UpdateNotificationViewModel
    {
        public NotificationStatus Status { get; set; } = NotificationStatus.Read;
    }

    public enum NotificationType
    {
        Generic,
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
}
