using Microsoft.EntityFrameworkCore;
using Npgsql;
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
        {
            return null;
        }

        return MapToDto(notification);
    }

    public async Task<IEnumerable<NotificationDTO>> GetUserNotificationsAsync(int userId, bool unreadOnly = false)
    {
        Console.WriteLine(
            $"[NotificationRepository] Getting notifications for userId: {userId}, unreadOnly: {unreadOnly}");

        var query = _context.Notifications
            .Where(n => n.UserID == userId);

        if (unreadOnly)
        {
            Console.WriteLine("[NotificationRepository] Filtering for unread notifications only");
            query = query.Where(n => n.Status == NotificationStatus.Unread);
        }

        // Log the generated SQL query
        var sql = query.ToQueryString();
        Console.WriteLine($"[NotificationRepository] SQL Query: {sql}");

        var notifications = await query
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();

        Console.WriteLine($"[NotificationRepository] Found {notifications.Count} notifications in database");

        var result = notifications.Select(MapToDto).ToList();
        Console.WriteLine($"[NotificationRepository] Mapped to {result.Count} DTOs");

        return result;
    }

    public async Task<NotificationDTO> CreateNotificationAsync(CreateNotificationDTO notificationDto)
    {
        // Add logging
        Console.WriteLine($"Creating notification for UserID: {notificationDto.UserID}");

        // Verify the user exists
        var user = await _context.Users.FindAsync(notificationDto.UserID);
        if (user == null)
        {
            Console.WriteLine($"User with ID {notificationDto.UserID} not found in database");
            throw new Exception($"User with ID {notificationDto.UserID} not found");
        }

        Console.WriteLine($"Found user: {user.Username} (ID: {user.UserID})");

        var notification = new Notification
        {
            UserID = notificationDto.UserID,
            Message = notificationDto.Message,
            Type = notificationDto.Type,
            Status = NotificationStatus.Unread,
            CreatedAt = DateTime.UtcNow
        };

        try
        {
            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();
            return MapToDto(notification);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException pgEx && pgEx.SqlState == "23503")
        {
            Console.WriteLine($"Foreign key violation. Details: {pgEx.Detail}");
            Console.WriteLine($"Table: {pgEx.TableName}, Constraint: {pgEx.ConstraintName}");
            throw new Exception(
                $"Foreign key violation. User with ID {notificationDto.UserID} might not exist or there's an issue with the database constraints.");
        }
    }


    public async Task<bool> MarkAsReadAsync(int notificationId, int userId)
    {
        var notification = await _context.Notifications
            .FirstOrDefaultAsync(n => n.NotificationID == notificationId && n.UserID == userId);

        if (notification == null)
        {
            return false;
        }

        notification.Status = NotificationStatus.Read;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task MarkAllAsReadAsync(int userId)
    {
        var unreadNotifications = await _context.Notifications
            .Where(n => n.UserID == userId && n.Status == NotificationStatus.Unread)
            .ToListAsync();

        foreach (var notification in unreadNotifications) notification.Status = NotificationStatus.Read;


        await _context.SaveChangesAsync();
    }

    public async Task<bool> DeleteNotificationAsync(int notificationId, int userId)
    {
        var notification = await _context.Notifications
            .FirstOrDefaultAsync(n => n.NotificationID == notificationId && n.UserID == userId);

        if (notification == null)
        {
            return false;
        }

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