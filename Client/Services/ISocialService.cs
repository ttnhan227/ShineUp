using Client.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Client.Services
{
    public interface ISocialService
    {
        // Comments
        Task<List<CommentViewModel>> GetCommentsForPostAsync(int postId);
        Task<CommentViewModel> AddCommentAsync(CommentViewModel comment);
        Task<bool> DeleteCommentAsync(int commentId);

        // Likes
        Task<List<LikeViewModel>> GetLikesForPostAsync(int postId);
        Task<LikeViewModel> ToggleLikeAsync(CreateLikeViewModel like);
        Task<bool> HasLikedPostAsync(int postId);
        Task<int> GetLikeCountAsync(int postId);
    }
}