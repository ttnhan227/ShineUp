using Server.Models;

namespace Server.Interfaces;

public interface ICommentRepository
{
    Task<Comment?> GetByIdAsync(int id);
    Task<List<Comment>> GetAllAsync();
    Task<List<Comment>> GetCommentsByPostIdAsync(int postId);
    Task<Comment> AddCommentAsync(Comment comment);
    Task UpdateAsync(Comment comment);
    Task DeleteAsync(int id);
    Task<bool> ExistsAsync(int id);
}