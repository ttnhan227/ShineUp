using Server.Models;
using System.Text.Json.Serialization;

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
    [JsonPropertyName("userID")]
    public int UserID { get; set; }
    
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;
    
    [JsonPropertyName("type")]
    [JsonConverter(typeof(JsonStringEnumConverter))]  // This enables string serialization
    public NotificationType Type { get; set; } = NotificationType.Generic;
    
    [JsonPropertyName("relatedEntityId")]
    public int? RelatedEntityId { get; set; }
    
    [JsonPropertyName("relatedEntityType")]
    public string? RelatedEntityType { get; set; }
}
public class UpdateNotificationDTO
{
    public NotificationStatus Status { get; set; } = NotificationStatus.Read;
}
