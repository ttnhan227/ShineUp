using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.DTOs;
using Server.Interfaces;
using Server.Models;

namespace Server.Repositories;

public class NotificationRepository : INotificationRepository
{
    private readonly DatabaseContext _context;

    public NotificationRepository(DatabaseContext context)
    {
        _context = context;
    }

    public async Task<NotificationDTO?> GetByIdAsync(int id, int userId)
    {
        var notification = await _context.Notifications
            .Include(n => n.User)
            .FirstOrDefaultAsync(n => n.NotificationID == id && n.UserID == userId);

        if (notification == null)
            return null;

        return MapToDto(notification);
    }

    public async Task<IEnumerable<NotificationDTO>> GetUserNotificationsAsync(int userId, bool unreadOnly = false)
    {
        var query = _context.Notifications
            .Where(n => n.UserID == userId);
            
        if (unreadOnly)
        {
            query = query.Where(n => n.Status == NotificationStatus.Unread);
        }

        var notifications = await query
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();

        return notifications.Select(MapToDto);
    }

    public async Task<NotificationDTO> CreateNotificationAsync(CreateNotificationDTO notificationDto)
    {
        var notification = new Notification
        {
            UserID = notificationDto.UserID,
            Message = notificationDto.Message,
            Type = notificationDto.Type,
            Status = NotificationStatus.Unread,
            CreatedAt = DateTime.UtcNow
        };

        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();

        return MapToDto(notification);
    }


    public async Task<bool> MarkAsReadAsync(int notificationId, int userId)
    {
        var notification = await _context.Notifications
            .FirstOrDefaultAsync(n => n.NotificationID == notificationId && n.UserID == userId);

        if (notification == null)
            return false;

        notification.Status = NotificationStatus.Read;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task MarkAllAsReadAsync(int userId)
    {
        var unreadNotifications = await _context.Notifications
            .Where(n => n.UserID == userId && n.Status == NotificationStatus.Unread)
            .ToListAsync();

        foreach (var notification in unreadNotifications)
        {
            notification.Status = NotificationStatus.Read;
        }


        await _context.SaveChangesAsync();
    }

    public async Task<bool> DeleteNotificationAsync(int notificationId, int userId)
    {
        var notification = await _context.Notifications
            .FirstOrDefaultAsync(n => n.NotificationID == notificationId && n.UserID == userId);

        if (notification == null)
            return false;

        _context.Notifications.Remove(notification);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<int> GetUnreadCountAsync(int userId)
    {
        return await _context.Notifications
            .CountAsync(n => n.UserID == userId && n.Status == NotificationStatus.Unread);
    }

    private static NotificationDTO MapToDto(Notification notification)
    {
        return new NotificationDTO
        {
            NotificationID = notification.NotificationID,
            UserID = notification.UserID,
            Message = notification.Message,
            Type = notification.Type,
            Status = notification.Status,
            CreatedAt = notification.CreatedAt,
            RelatedEntity = null // Set this based on your business logic if needed
        };
    }
}
