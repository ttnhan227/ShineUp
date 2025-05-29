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
            .Include(p => p.Images)
            .Include(p => p.Videos)
            .Include(p => p.Likes)
            .Include(p => p.Comments)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
    }

    public async Task<Post> GetPostByIdAsync(int id)
    {
        return await _context.Posts
            .Include(p => p.User)
            .Include(p => p.Category)
            .Include(p => p.Privacy)
            .Include(p => p.Images)
            .Include(p => p.Videos)
            .Include(p => p.Likes)
            .Include(p => p.Comments)
            .FirstOrDefaultAsync(p => p.PostID == id);
    }

    public async Task<Post> CreatePostAsync(Post post)
    {
        _context.Posts.Add(post);
        await _context.SaveChangesAsync();
        return post;
    }

    public async Task<Post> UpdatePostAsync(Post post)
    {
        _context.Entry(post).State = EntityState.Modified;
        await _context.SaveChangesAsync();
        return post;
    }

    public async Task DeletePostAsync(Post post)
    {
        _context.Posts.Remove(post);
        await _context.SaveChangesAsync();
    }

    public async Task<IEnumerable<Post>> GetPostsByUserIdAsync(int userId)
    {
        return await _context.Posts
            .Include(p => p.User)
            .Include(p => p.Category)
            .Include(p => p.Privacy)
            .Include(p => p.Images)
            .Include(p => p.Videos)
            .Include(p => p.Likes)
            .Include(p => p.Comments)
            .Where(p => p.UserID == userId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Post>> GetPostsByCategoryAsync(int categoryId)
    {
        return await _context.Posts
            .Include(p => p.User)
            .Include(p => p.Category)
            .Include(p => p.Privacy)
            .Include(p => p.Images)
            .Include(p => p.Videos)
            .Include(p => p.Likes)
            .Include(p => p.Comments)
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
            .Include(p => p.Images)
            .Include(p => p.Videos)
            .Include(p => p.Likes)
            .Include(p => p.Comments)
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

    public async Task<User> GetUserByIdAsync(int userId)
    {
        return await _context.Users.FindAsync(userId);
    }

    public async Task<Image> AddImageAsync(Image image)
    {
        _context.Images.Add(image);
        await _context.SaveChangesAsync();
        return image;
    }

    public async Task<Video> AddVideoAsync(Video video)
    {
        _context.Videos.Add(video);
        await _context.SaveChangesAsync();
        return video;
    }

    public async Task RemoveAllMediaFromPostAsync(int postId)
    {
        var post = await _context.Posts
            .Include(p => p.Images)
            .Include(p => p.Videos)
            .FirstOrDefaultAsync(p => p.PostID == postId);

        if (post != null)
        {
            _context.Images.RemoveRange(post.Images);
            _context.Videos.RemoveRange(post.Videos);
            await _context.SaveChangesAsync();
        }
    }
} 