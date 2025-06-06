using Server.DTOs;
using Server.Models;

namespace Server.Interfaces;

public interface INotificationRepository
{
    Task<NotificationDTO?> GetByIdAsync(int id, int userId);
    Task<IEnumerable<NotificationDTO>> GetUserNotificationsAsync(int userId, bool unreadOnly = false);
    Task<NotificationDTO> CreateNotificationAsync(CreateNotificationDTO notificationDto);
    Task<bool> MarkAsReadAsync(int notificationId, int userId);
    Task MarkAllAsReadAsync(int userId);
    Task<bool> DeleteNotificationAsync(int notificationId, int userId);
    Task<int> GetUnreadCountAsync(int userId);
}
