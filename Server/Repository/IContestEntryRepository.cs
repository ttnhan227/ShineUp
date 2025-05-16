using Server.Models;

namespace Server.Repository
{
    public interface IContestEntryRepository
    {
        Task<bool> HasSubmittedAsync(int contestId, int userId);
        Task<List<object>> GetEntriesByContestAsync(int contestId);
        Task AddAsync(ContestEntry entry);
    }
}
