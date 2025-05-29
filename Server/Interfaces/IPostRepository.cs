using Server.Models;

namespace Server.Interfaces;

public interface IPostRepository
{
    // Basic CRUD
    Task<IEnumerable<Post>> GetAllPostsAsync();
    Task<Post?> GetPostByIdAsync(int postId);
    Task<Post> CreatePostAsync(Post post);
    Task<Post?> UpdatePostAsync(Post post);
    Task<bool> DeletePostAsync(int postId);

    // Additional methods
    Task<IEnumerable<Post>> GetPostsByUserIdAsync(int userId);
    Task<IEnumerable<Post>> GetPostsByCategoryAsync(int categoryId);
    Task<IEnumerable<Post>> GetRecentPostsAsync(int count);
    Task<bool> PostExistsAsync(int postId);
    
    // Social features
    Task<int> GetPostLikesCountAsync(int postId);
    Task<int> GetPostCommentsCountAsync(int postId);
} 