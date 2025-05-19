using Server.Models;

namespace Server.Interfaces
{
    public interface IPrivacyRepository
    {
        Task<Privacy?> GetByIdAsync(int id);
        Task<List<Privacy>> GetAllAsync();
        Task AddAsync(Privacy privacy);
        Task UpdateAsync(Privacy privacy);
        Task DeleteAsync(int id);
    }
}
