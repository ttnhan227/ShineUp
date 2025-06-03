using System;
using System.ComponentModel.DataAnnotations;

namespace Client.Models
{
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

    public class NotificationViewModel
    {
        public int NotificationID { get; set; }
        public int UserID { get; set; }
        public string Message { get; set; } = string.Empty;
        public NotificationType Type { get; set; }
        public NotificationStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? RelatedEntityType { get; set; } // e.g., "Opportunity", "Application", "Message"
        public int? RelatedEntityId { get; set; }    // ID of the related entity
        public string? SenderName { get; set; }      // Name of the user who triggered the notification
        public string? SenderImage { get; set; }     // Profile image of the sender
        public bool IsActionable { get; set; }       // Whether the notification requires user action
    }

    public class CreateNotificationViewModel
    {
        [Required]
        public int UserID { get; set; }
        
        [Required(ErrorMessage = "Message is required")]
        [StringLength(500, ErrorMessage = "Message cannot be longer than 500 characters")]
        public string Message { get; set; } = string.Empty;
        
        public NotificationType Type { get; set; } = NotificationType.Generic;
        
        public string? RelatedEntityType { get; set; }
        public int? RelatedEntityId { get; set; }
        public int? SenderId { get; set; }
        public bool IsActionable { get; set; } = false;
    }

    public class UpdateNotificationStatusViewModel
    {
        [Required]
        public int NotificationID { get; set; }
        
        [Required]
        public NotificationStatus Status { get; set; }
    }

    public class NotificationPreferencesViewModel
    {
        public bool EmailEnabled { get; set; } = true;
        public bool PushEnabled { get; set; } = true;
        public bool InAppEnabled { get; set; } = true;
        
        // Notification type specific preferences
        public bool NotifyOnOpportunityPosted { get; set; } = true;
        public bool NotifyOnApplicationUpdate { get; set; } = true;
        public bool NotifyOnMessageReceived { get; set; } = true;
        public bool NotifyOnTalentMatch { get; set; } = true;
        public bool NotifyOnSystemAlert { get; set; } = true;
    }
}
