using Server.DTOs;

namespace Server.Interfaces;

public interface ISocialService
{
    // Comments
    Task<List<CommentDto>> GetCommentsForPostAsync(int postId);
    Task<CommentDto> AddCommentAsync(CommentDto comment);
    Task<bool> DeleteCommentAsync(int commentId);

    // Likes
    Task<List<LikeDto>> GetLikesForPostAsync(int postId);
    Task<LikeDto> ToggleLikeAsync(LikeDto like);
    Task<bool> HasLikedPostAsync(int postId, int userId);
    Task<int> GetLikeCountAsync(int postId);
}