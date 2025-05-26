using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Interfaces;
using Server.Models;

namespace Server.Repositories;

public class ShareRepository : IShareRepository
{
    private readonly DatabaseContext _context;

    public ShareRepository(DatabaseContext context)
    {
        _context = context;
    }

    public async Task AddAsync(Share share)
    {
        _context.Shares.Add(share);
        await _context.SaveChangesAsync();
    }

    public async Task<bool> HasSharedAsync(Guid videoId, int userId)
    {
        return await _context.Shares
            .AnyAsync(s => s.VideoID == videoId && s.UserID == userId);
    }

    public async Task<List<Share>> GetByVideoIdAsync(Guid videoId)
    {
        return await _context.Shares
            .Where(s => s.VideoID == videoId)
            .ToListAsync();
    }
}