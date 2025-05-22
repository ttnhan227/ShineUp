using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Interfaces;
using Server.Models;

namespace Server.Repositories;

public class LikeRepository : ILikeRepository
{
    private readonly DatabaseContext _context;

    public LikeRepository(DatabaseContext context)
    {
        _context = context;
    }

    public async Task<Like?> GetByIdAsync(int id)
    {
        return await _context.Likes.FindAsync(id);
    }

    public async Task<List<Like>> GetAllAsync()
    {
        return await _context.Likes.ToListAsync();
    }

    public async Task AddAsync(Like like)
    {
        _context.Likes.Add(like);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Like like)
    {
        _context.Entry(like).State = EntityState.Modified;
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var likeToDelete = await _context.Likes.FindAsync(id);
        if (likeToDelete != null)
        {
            _context.Likes.Remove(likeToDelete);
            await _context.SaveChangesAsync();
        }
    }
}