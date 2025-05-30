using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.DTOs;
using Server.Interfaces;
using Server.Models;

namespace Server.Repositories;

public class LikeRepository : ILikeRepository
{
    private readonly DatabaseContext _context;
    private readonly ILogger<LikeRepository> _logger;

    public LikeRepository(DatabaseContext context, ILogger<LikeRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Like?> GetByIdAsync(int id)
    {
        try
        {
            return await _context.Likes
                .Include(l => l.User)
                .FirstOrDefaultAsync(l => l.LikeID == id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting like by ID: {LikeId}", id);
            throw;
        }
    }

    public async Task<List<Like>> GetAllAsync()
    {
        try
        {
            return await _context.Likes
                .Include(l => l.User)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all likes");
            throw;
        }
    }

    public async Task<List<Like>> GetLikesByPostIdAsync(int postId)
    {
        try
        {
            return await _context.Likes
                .Where(l => l.PostID == postId)
                .Include(l => l.User)
                .OrderByDescending(l => l.CreatedAt)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting likes for post ID: {PostId}", postId);
            throw;
        }
    }

    public async Task<Like> AddLikeAsync(Like like)
    {
        try
        {
            like.CreatedAt = DateTime.UtcNow;
            _context.Likes.Add(like);
            await _context.SaveChangesAsync();
            return like;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding like");
            throw;
        }
    }

    public async Task RemoveLikeAsync(Like like)
    {
        try
        {
            _context.Likes.Remove(like);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing like ID: {LikeId}", like.LikeID);
            throw;
        }
    }

    public async Task UpdateAsync(Like like)
    {
        try
        {
            _context.Entry(like).State = EntityState.Modified;
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating like ID: {LikeId}", like.LikeID);
            throw;
        }
    }

    public async Task DeleteAsync(int id)
    {
        try
        {
            var likeToDelete = await _context.Likes.FindAsync(id);
            if (likeToDelete != null)
            {
                _context.Likes.Remove(likeToDelete);
                await _context.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting like ID: {LikeId}", id);
            throw;
        }
    }

    public async Task<bool> ExistsAsync(int id)
    {
        return await _context.Likes.AnyAsync(l => l.LikeID == id);
    }

    public async Task<bool> UserLikedPostAsync(int userId, int postId)
    {
        return await _context.Likes
            .AnyAsync(l => l.UserID == userId && l.PostID == postId);
    }
}