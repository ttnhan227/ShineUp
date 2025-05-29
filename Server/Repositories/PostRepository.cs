using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Interfaces;
using Server.Models;

namespace Server.Repositories;

public class PostRepository : IPostRepository
{
    private readonly DatabaseContext _context;

    public PostRepository(DatabaseContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Post>> GetAllPostsAsync()
    {
        return await _context.Posts
            .Include(p => p.User)
            .Include(p => p.Category)
            .Include(p => p.Privacy)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
    }

    public async Task<Post?> GetPostByIdAsync(int postId)
    {
        return await _context.Posts
            .Include(p => p.User)
            .Include(p => p.Category)
            .Include(p => p.Privacy)
            .Include(p => p.Comments)
            .Include(p => p.Likes)
            .FirstOrDefaultAsync(p => p.PostID == postId);
    }

    public async Task<Post> CreatePostAsync(Post post)
    {
        await _context.Posts.AddAsync(post);
        await _context.SaveChangesAsync();
        
        // Reload the post with related entities
        return await _context.Posts
            .Include(p => p.User)
            .Include(p => p.Category)
            .Include(p => p.Privacy)
            .FirstOrDefaultAsync(p => p.PostID == post.PostID) ?? post;
    }

    public async Task<Post?> UpdatePostAsync(Post post)
    {
        var existingPost = await _context.Posts.FindAsync(post.PostID);
        if (existingPost == null) return null;

        _context.Entry(existingPost).CurrentValues.SetValues(post);
        existingPost.UpdatedAt = DateTime.UtcNow;
        
        await _context.SaveChangesAsync();
        return existingPost;
    }

    public async Task<bool> DeletePostAsync(int postId)
    {
        var post = await _context.Posts.FindAsync(postId);
        if (post == null) return false;

        _context.Posts.Remove(post);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<IEnumerable<Post>> GetPostsByUserIdAsync(int userId)
    {
        return await _context.Posts
            .Include(p => p.Category)
            .Include(p => p.Privacy)
            .Where(p => p.UserID == userId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Post>> GetPostsByCategoryAsync(int categoryId)
    {
        return await _context.Posts
            .Include(p => p.User)
            .Include(p => p.Privacy)
            .Where(p => p.CategoryID == categoryId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Post>> GetRecentPostsAsync(int count)
    {
        return await _context.Posts
            .Include(p => p.User)
            .Include(p => p.Category)
            .Include(p => p.Privacy)
            .OrderByDescending(p => p.CreatedAt)
            .Take(count)
            .ToListAsync();
    }

    public async Task<bool> PostExistsAsync(int postId)
    {
        return await _context.Posts.AnyAsync(p => p.PostID == postId);
    }

    public async Task<int> GetPostLikesCountAsync(int postId)
    {
        return await _context.Likes
            .CountAsync(l => l.PostID == postId);
    }

    public async Task<int> GetPostCommentsCountAsync(int postId)
    {
        return await _context.Comments
            .CountAsync(c => c.PostID == postId);
    }
} 