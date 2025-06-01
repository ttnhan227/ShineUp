using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Interfaces;
using Server.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Server.Repositories;

public class NotificationRepository : INotificationRepository
{
    private readonly DatabaseContext _context;

    public NotificationRepository(DatabaseContext context)
    {
        _context = context;
    }

    public async Task<Notification> CreateAsync(Notification notification)
    {
        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();
        return notification;
    }

    public async Task<IEnumerable<Notification>> GetByUserIdAsync(int userId)
    {
        return await _context.Notifications
            .Include(n => n.User)
            .Where(n => n.UserID == userId)
            .ToListAsync();
    }

    public async Task<bool> DeleteAsync(int notificationId)
    {
        var notification = await _context.Notifications.FindAsync(notificationId);
        if (notification == null)
            return false;

        _context.Notifications.Remove(notification);
        await _context.SaveChangesAsync();
        return true;
    }
}