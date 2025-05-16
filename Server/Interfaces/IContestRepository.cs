using Server.Models;

namespace Server.Interfaces
{
    public interface IContestRepository
    {
        Task<List<Contest>> GetAllAsync();
        Task<Contest?> GetByIdAsync(int id);
        Task AddAsync(Contest contest);
        Task UpdateAsync(Contest contest);
        Task DeleteAsync(int id);
    }
}
