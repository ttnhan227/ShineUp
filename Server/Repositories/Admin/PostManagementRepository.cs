using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.DTOs.Admin;
using Server.Interfaces.Admin;

namespace Server.Repositories.Admin;

public class PostManagementRepository : IPostManagementRepository
{
    private readonly DatabaseContext _context;
    private readonly ILogger<PostManagementRepository> _logger;

    public PostManagementRepository(DatabaseContext context, ILogger<PostManagementRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IEnumerable<AdminPostDTO>> GetAllPostsAsync()
    {
        try
        {
            return await _context.Posts
                .Include(p => p.User)
                .Include(p => p.Category)
                .Include(p => p.Privacy)
                .Include(p => p.Community)
                .Include(p => p.Comments)
                .Include(p => p.Likes)
                .Select(p => new AdminPostDTO
                {
                    PostID = p.PostID,
                    Title = p.Title,
                    Content = p.Content,
                    CreatedAt = p.CreatedAt,
                    UpdatedAt = p.UpdatedAt,
                    UserID = p.UserID,
                    UserName = p.User.Username,
                    UserEmail = p.User.Email,
                    CategoryID = p.CategoryID,
                    CategoryName = p.Category != null ? p.Category.CategoryName : null,
                    PrivacyID = p.PrivacyID,
                    PrivacyName = p.Privacy != null ? p.Privacy.Name : null,
                    CommunityID = p.CommunityID,
                    CommunityName = p.Community != null ? p.Community.Name : null,
                    CommentCount = p.Comments != null ? p.Comments.Count : 0,
                    LikeCount = p.Likes != null ? p.Likes.Count : 0,
                    IsActive = true // Default to true since there's no IsActive in the model
                })
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all posts");
            throw;
        }
    }

    public async Task<AdminPostDTO> GetPostByIdAsync(int postId)
    {
        try
        {
            return await _context.Posts
                .Where(p => p.PostID == postId)
                .Include(p => p.User)
                .Include(p => p.Category)
                .Include(p => p.Privacy)
                .Include(p => p.Community)
                .Include(p => p.Comments)
                .Include(p => p.Likes)
                .Select(p => new AdminPostDTO
                {
                    PostID = p.PostID,
                    Title = p.Title,
                    Content = p.Content,
                    CreatedAt = p.CreatedAt,
                    UpdatedAt = p.UpdatedAt,
                    UserID = p.UserID,
                    UserName = p.User.Username,
                    UserEmail = p.User.Email,
                    CategoryID = p.CategoryID,
                    CategoryName = p.Category != null ? p.Category.CategoryName : null,
                    PrivacyID = p.PrivacyID,
                    PrivacyName = p.Privacy != null ? p.Privacy.Name : null,
                    CommunityID = p.CommunityID,
                    CommunityName = p.Community != null ? p.Community.Name : null,
                    CommentCount = p.Comments != null ? p.Comments.Count : 0,
                    LikeCount = p.Likes != null ? p.Likes.Count : 0,
                    IsActive = true // Default to true since there's no IsActive in the model
                })
                .FirstOrDefaultAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting post with ID {postId}");
            throw;
        }
    }

    public async Task<bool> UpdatePostStatusAsync(int postId, bool isActive)
    {
        try
        {
            var post = await _context.Posts.FindAsync(postId);
            if (post == null)
            {
                return false;
            }

            // Since there's no IsActive in the model, we'll just update the UpdatedAt
            // If you want to implement soft delete, you might need to add an IsActive field to the Post model
            post.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error updating post ID {postId}: {ex.Message}");
            throw;
        }
    }

    public async Task<bool> DeletePostAsync(int postId)
    {
        try
        {
            var post = await _context.Posts.FindAsync(postId);
            if (post == null)
            {
                return false;
            }

            _context.Posts.Remove(post);
            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error deleting post with ID {postId}");
            throw;
        }
    }
}