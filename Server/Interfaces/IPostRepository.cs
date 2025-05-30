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
    
    // Social features
    Task<int> GetPostLikesCountAsync(int postId);
    Task<int> GetPostCommentsCountAsync(int postId);

    // Media handling
    Task<User> GetUserByIdAsync(int userId);
    Task<Image> AddImageAsync(Image image);
    Task<Video> AddVideoAsync(Video video);
    Task RemoveAllMediaFromPostAsync(int postId);
} 