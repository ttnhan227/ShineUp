using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.DTOs;
using Server.Interfaces;
using Server.Models;

namespace Server.Services;

public class SocialService : ISocialService
{
    private readonly DatabaseContext _context;
    private readonly ILogger<SocialService> _logger;

    public SocialService(DatabaseContext context, ILogger<SocialService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<CommentDto>> GetCommentsForPostAsync(int postId)
    {
        try
        {
            return await _context.Comments
                .Where(c => c.PostID == postId)
                .OrderByDescending(c => c.CreatedAt)
                .Select(c => new CommentDto
                {
                    CommentID = c.CommentID,
                    PostID = c.PostID,
                    VideoID = c.VideoID,
                    UserID = c.UserID,
                    Content = c.Content,
                    CreatedAt = c.CreatedAt,
                    Username = c.User.Username,
                    ProfileImageURL = c.User.ProfileImageURL
                })
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting comments for post {PostId}", postId);
            throw;
        }
    }

    public async Task<CommentDto> AddCommentAsync(CommentDto comment)
    {
        try
        {
            var newComment = new Comment
            {
                PostID = comment.PostID,
                VideoID = comment.VideoID,
                UserID = comment.UserID,
                Content = comment.Content,
                CreatedAt = DateTime.UtcNow
            };

            _context.Comments.Add(newComment);
            await _context.SaveChangesAsync();

            // Get the created comment with user data
            return await _context.Comments
                .Where(c => c.CommentID == newComment.CommentID)
                .Select(c => new CommentDto
                {
                    CommentID = c.CommentID,
                    PostID = c.PostID,
                    VideoID = c.VideoID,
                    UserID = c.UserID,
                    Content = c.Content,
                    CreatedAt = c.CreatedAt,
                    Username = c.User.Username,
                    ProfileImageURL = c.User.ProfileImageURL
                })
                .FirstOrDefaultAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding comment");
            throw;
        }
    }

    public async Task<bool> DeleteCommentAsync(int commentId)
    {
        try
        {
            var comment = await _context.Comments.FindAsync(commentId);
            if (comment == null)
            {
                return false;
            }

            _context.Comments.Remove(comment);
            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting comment {CommentId}", commentId);
            throw;
        }
    }

    public async Task<List<LikeDto>> GetLikesForPostAsync(int postId)
    {
        try
        {
            return await _context.Likes
                .Where(l => l.PostID == postId)
                .OrderByDescending(l => l.CreatedAt)
                .Select(l => new LikeDto
                {
                    LikeID = l.LikeID,
                    PostID = l.PostID,
                    VideoID = l.VideoID,
                    UserID = l.UserID,
                    CreatedAt = l.CreatedAt,
                    Username = l.User.Username,
                    ProfileImageURL = l.User.ProfileImageURL
                })
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting likes for post {PostId}", postId);
            throw;
        }
    }

    public async Task<LikeDto> ToggleLikeAsync(LikeDto likeDto)
    {
        try
        {
            // Check if like already exists
            var existingLike = await _context.Likes
                .FirstOrDefaultAsync(l => l.PostID == likeDto.PostID && l.UserID == likeDto.UserID);

            if (existingLike != null)
            {
                // Unlike
                _context.Likes.Remove(existingLike);
                await _context.SaveChangesAsync();
                return null; // Return null to indicate the like was removed
            }

            // Add new like
            var newLike = new Like
            {
                PostID = likeDto.PostID,
                VideoID = likeDto.VideoID,
                UserID = likeDto.UserID,
                CreatedAt = DateTime.UtcNow
            };

            _context.Likes.Add(newLike);
            await _context.SaveChangesAsync();

            // Return the created like with user data
            return await _context.Likes
                .Where(l => l.LikeID == newLike.LikeID)
                .Select(l => new LikeDto
                {
                    LikeID = l.LikeID,
                    PostID = l.PostID,
                    VideoID = l.VideoID,
                    UserID = l.UserID,
                    CreatedAt = l.CreatedAt,
                    Username = l.User.Username,
                    ProfileImageURL = l.User.ProfileImageURL
                })
                .FirstOrDefaultAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error toggling like");
            throw;
        }
    }

    public async Task<bool> HasLikedPostAsync(int postId, int userId)
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

    public async Task<int> GetLikeCountAsync(int postId)
    {
        try
        {
            return await _context.Likes
                .CountAsync(l => l.PostID == postId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting like count for post {PostId}", postId);
            throw;
        }
    }
}