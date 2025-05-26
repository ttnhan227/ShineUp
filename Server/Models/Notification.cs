using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Server.Models;

public class Notification
{
    [Key]
    public int NotificationID { get; set; }

    [ForeignKey("User")]
    public int UserID { get; set; }

    public User User { get; set; }

    [Required]
    public string NotificationType { get; set; } // e.g., "Vote", "Message", "ContestUpdate", "Like", "Share", "Comment"

    [Required]
    public string Message { get; set; }

    public Guid? VideoID { get; set; } // Optional, for video-related notifications
    public int? ContestID { get; set; } // Optional, for contest-related notifications
    public int? MessageID { get; set; } // Optional, for message notifications
    public int? CommentID { get; set; } // Optional, for comment notifications
    public int? TriggeredByUserID { get; set; } // User who triggered the notification (e.g., who liked or commented)

    public DateTime CreatedAt { get; set; }
    public bool IsRead { get; set; } = false;
}