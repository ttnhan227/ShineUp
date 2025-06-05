using Server.DTOs;
using Server.Models;

namespace Server.Interfaces;

public interface IPostRepository
{
    // Basic CRUD
    Task<IEnumerable<Post>> GetAllPostsAsync();
    Task<IEnumerable<Post>> GetVisiblePostsAsync(int? userId = null);
    Task<Post> GetPostByIdAsync(int id);
    Task<Post> CreatePostAsync(Post post);
    Task<Post> UpdatePostAsync(Post post);
    Task DeletePostAsync(Post post);

    // Additional methods
    Task<IEnumerable<Post>> GetPostsByUserIdAsync(int userId);
    Task<IEnumerable<Post>> GetPostsByCategoryAsync(int categoryId);
    Task<IEnumerable<Post>> GetRecentPostsAsync(int count);
    Task<bool> PostExistsAsync(int postId);
    
    // Social features - Comments
    Task<Comment> AddCommentToPostAsync(Comment comment);
    Task<IEnumerable<Comment>> GetCommentsForPostAsync(int postId);
    Task<Comment> GetCommentByIdAsync(int commentId);
    Task<bool> DeleteCommentAsync(int commentId, int userId);
    Task<int> GetPostCommentsCountAsync(int postId);

    // Social features - Likes
    Task<Like> LikePostAsync(Like like);
    Task<bool> UnlikePostAsync(int postId, int userId);
    Task<bool> HasUserLikedPostAsync(int postId, int userId);
    Task<int> GetPostLikesCountAsync(int postId);
    Task<IEnumerable<Like>> GetLikesForPostAsync(int postId);

    // Media handling
    Task<User> GetUserByIdAsync(int userId);
    Task<Image> AddImageAsync(Image image);
    Task<Video> AddVideoAsync(Video video);
    Task RemoveAllMediaFromPostAsync(int postId);
    
    //Hoang Community features
    Task<List<Post>> GetPostsByCommunityIdAsync(int communityId);

}