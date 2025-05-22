using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Interfaces;
using Server.Models;

namespace Server.Repositories;

public class MessageRepository : IMessageRepository
{
    private readonly DatabaseContext _context;

    public MessageRepository(DatabaseContext context)
    {
        _context = context;
    }

    public async Task<Message?> GetByIdAsync(int id)
    {
        return await _context.Messages.FindAsync(id);
    }

    public async Task<List<Message>> GetAllAsync()
    {
        return await _context.Messages.ToListAsync();
    }

    public async Task AddAsync(Message message)
    {
        _context.Messages.Add(message);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Message message)
    {
        _context.Entry(message).State = EntityState.Modified;
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var messageToDelete = await _context.Messages.FindAsync(id);
        if (messageToDelete != null)
        {
            _context.Messages.Remove(messageToDelete);
            await _context.SaveChangesAsync();
        }
    }
}