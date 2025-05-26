using Server.Models;

namespace Server.Interfaces;

public interface INotificationRepository
{
    Task AddAsync(Notification notification);
    Task<List<Notification>> GetByUserIdAsync(int userId, bool includeRead = true);
    Task MarkAsReadAsync(int notificationId);
}