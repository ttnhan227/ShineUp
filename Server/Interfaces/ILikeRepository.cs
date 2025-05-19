using Server.Models;

namespace Server.Interfaces
{
    public interface ILikeRepository
    {
        Task<Like?> GetByIdAsync(int id);
        Task<List<Like>> GetAllAsync();
        Task AddAsync(Like like);
        Task UpdateAsync(Like like);
        Task DeleteAsync(int id);
    }
}
