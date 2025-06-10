using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Interfaces;
using Server.Models;

namespace Server.Repositories;

public class PostRepository : IPostRepository
{
    private readonly DatabaseContext _context;
    private readonly ILogger<PostRepository> _logger;

    public PostRepository(DatabaseContext context, ILogger<PostRepository> logger)
    {
        _context = context;
        _logger = logger;
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

    public async Task<IEnumerable<Post>> GetVisiblePostsAsync(int? userId = null)
    {
        var query = _context.Posts.AsQueryable();

        // Apply privacy filter first
        if (userId == null)
        {
            // For non-authenticated users, only show public posts
            query = query.Where(p => p.Privacy.Name == "Public");
        }
        else
        {
            // For authenticated users, show:
            // 1. All public posts
            // 2. Their own posts (regardless of privacy setting)
            query = query.Where(p =>
                p.Privacy.Name == "Public" ||
                p.UserID == userId.Value
            );
        }

        // Then include related entities
        query = query
            .Include(p => p.User)
            .Include(p => p.Category)
            .Include(p => p.Privacy)
            .Include(p => p.Images)
            .Include(p => p.Videos)
            .Include(p => p.Likes)
            .Include(p => p.Comments)
            .OrderByDescending(p => p.CreatedAt);

        return await query.ToListAsync();
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
        var query = _context.Posts.AsQueryable();

        // For user profile pages, show all posts of that user
        // The privacy check will be handled in the controller
        query = query.Where(p => p.UserID == userId);

        // Then include related entities
        query = query
            .Include(p => p.User)
            .Include(p => p.Category)
            .Include(p => p.Privacy)
            .Include(p => p.Images)
            .Include(p => p.Videos)
            .Include(p => p.Likes)
            .Include(p => p.Comments)
            .OrderByDescending(p => p.CreatedAt);

        return await query.ToListAsync();
    }

    public async Task<IEnumerable<Post>> GetPostsByCategoryAsync(int categoryId)
    {
        var query = _context.Posts.AsQueryable();

        // Apply filters first
        query = query.Where(p => p.CategoryID == categoryId && p.Privacy.Name == "Public");

        // Then include related entities
        query = query
            .Include(p => p.User)
            .Include(p => p.Category)
            .Include(p => p.Privacy)
            .Include(p => p.Images)
            .Include(p => p.Videos)
            .Include(p => p.Likes)
            .Include(p => p.Comments)
            .OrderByDescending(p => p.CreatedAt);

        return await query.ToListAsync();
    }

    public async Task<IEnumerable<Post>> GetRecentPostsAsync(int count)
    {
        var query = _context.Posts.AsQueryable();

        // Apply privacy filter first
        query = query.Where(p => p.Privacy.Name == "Public");

        // Then include related entities
        query = query
            .Include(p => p.User)
            .Include(p => p.Category)
            .Include(p => p.Privacy)
            .Include(p => p.Images)
            .Include(p => p.Videos)
            .Include(p => p.Likes)
            .Include(p => p.Comments)
            .OrderByDescending(p => p.CreatedAt)
            .Take(count);

        return await query.ToListAsync();
    }

    public async Task<bool> PostExistsAsync(int postId)
    {
        return await _context.Posts.AnyAsync(p => p.PostID == postId);
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

    public async Task<List<Post>> GetPostsByCommunityIdAsync(int communityId)
    {
        return await _context.Posts
            .Where(p => p.CommunityID == communityId)
            .Include(p => p.User)
            .Include(p => p.Comments)
            .Include(p => p.Likes)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
    }

    #region Comment Methods

    public async Task<Comment> AddCommentToPostAsync(Comment comment)
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
            _logger.LogError(ex, "Error adding comment to post ID: {PostId}", comment.PostID);
            throw;
        }
    }

    public async Task<IEnumerable<Comment>> GetCommentsForPostAsync(int postId)
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

    public async Task<Comment> GetCommentByIdAsync(int commentId)
    {
        try
        {
            return await _context.Comments
                .Include(c => c.User)
                .FirstOrDefaultAsync(c => c.CommentID == commentId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting comment by ID: {CommentId}", commentId);
            throw;
        }
    }

    public async Task<bool> DeleteCommentAsync(int commentId, int userId)
    {
        try
        {
            var comment = await _context.Comments.FindAsync(commentId);
            if (comment == null)
            {
                return false;
            }

            // Only the comment owner or post owner can delete the comment
            var post = await _context.Posts.FindAsync(comment.PostID);
            if (comment.UserID != userId && (post == null || post.UserID != userId))
            {
                return false;
            }

            _context.Comments.Remove(comment);
            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting comment ID: {CommentId}", commentId);
            throw;
        }
    }

    public async Task<int> GetPostCommentsCountAsync(int postId)
    {
        try
        {
            return await _context.Comments.CountAsync(c => c.PostID == postId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting comment count for post ID: {PostId}", postId);
            throw;
        }
    }

    #endregion

    #region Like Methods

    public async Task<Like> LikePostAsync(Like like)
    {
        try
        {
            // Check if the user already liked the post
            var existingLike = await _context.Likes
                .FirstOrDefaultAsync(l => l.UserID == like.UserID && l.PostID == like.PostID);

            if (existingLike != null)
            {
                return existingLike; // Already liked
            }

            like.CreatedAt = DateTime.UtcNow;
            _context.Likes.Add(like);
            await _context.SaveChangesAsync();
            return like;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error liking post ID: {PostId}", like.PostID);
            throw;
        }
    }

    public async Task<bool> UnlikePostAsync(int postId, int userId)
    {
        try
        {
            var like = await _context.Likes
                .FirstOrDefaultAsync(l => l.PostID == postId && l.UserID == userId);

            if (like == null)
            {
                return false;
            }

            _context.Likes.Remove(like);
            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unliking post ID: {PostId}", postId);
            throw;
        }
    }

    public async Task<bool> HasUserLikedPostAsync(int postId, int userId)
    {
        try
        {
            return await _context.Likes
                .AnyAsync(l => l.PostID == postId && l.UserID == userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if user {UserId} liked post {PostId}", userId, postId);
            throw;
        }
    }

    public async Task<int> GetPostLikesCountAsync(int postId)
    {
        try
        {
            return await _context.Likes.CountAsync(l => l.PostID == postId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting like count for post ID: {PostId}", postId);
            throw;
        }
    }

    public async Task<IEnumerable<Like>> GetLikesForPostAsync(int postId)
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

    #endregion
}