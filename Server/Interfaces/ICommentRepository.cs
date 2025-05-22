using Server.Models;

namespace Server.Interfaces;

public interface ICommentRepository
{
    Task<Comment?> GetByIdAsync(int id);
    Task<List<Comment>> GetAllAsync();
    Task AddAsync(Comment comment);
    Task UpdateAsync(Comment comment);
    Task DeleteAsync(int id);
}