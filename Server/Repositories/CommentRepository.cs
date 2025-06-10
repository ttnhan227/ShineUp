using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Interfaces;
using Server.Models;

namespace Server.Repositories;

public class CommentRepository : ICommentRepository
{
    private readonly DatabaseContext _context;
    private readonly ILogger<CommentRepository> _logger;

    public CommentRepository(DatabaseContext context, ILogger<CommentRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Comment?> GetByIdAsync(int id)
    {
        try
        {
            return await _context.Comments
                .Include(c => c.User)
                .FirstOrDefaultAsync(c => c.CommentID == id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting comment by ID: {CommentId}", id);
            throw;
        }
    }

    public async Task<List<Comment>> GetAllAsync()
    {
        try
        {
            return await _context.Comments
                .Include(c => c.User)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all comments");
            throw;
        }
    }

    public async Task<List<Comment>> GetCommentsByPostIdAsync(int postId)
    {
        try
        {
            return await _context.Comments
                .Where(c => c.PostID == postId)
                .Include(c => c.User)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting comments for post ID: {PostId}", postId);
            throw;
        }
    }

    public async Task<Comment> AddCommentAsync(Comment comment)
    {
        try
        {
            comment.CreatedAt = DateTime.UtcNow;
            _context.Comments.Add(comment);
            await _context.SaveChangesAsync();
            return comment;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding comment");
            throw;
        }
    }

    public async Task UpdateAsync(Comment comment)
    {
        try
        {
            _context.Entry(comment).State = EntityState.Modified;
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating comment ID: {CommentId}", comment.CommentID);
            throw;
        }
    }

    public async Task DeleteAsync(int id)
    {
        try
        {
            var commentToDelete = await _context.Comments.FindAsync(id);
            if (commentToDelete != null)
            {
                _context.Comments.Remove(commentToDelete);
                await _context.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting comment ID: {CommentId}", id);
            throw;
        }
    }

    public async Task<bool> ExistsAsync(int id)
    {
        return await _context.Comments.AnyAsync(c => c.CommentID == id);
    }
}