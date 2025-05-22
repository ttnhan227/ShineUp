using Server.Models;

namespace Server.Interfaces;

public interface IContestEntryRepositories
{
    Task<bool> HasSubmittedAsync(int contestId, int userId);
    Task<List<object>> GetEntriesByContestAsync(int contestId);
    Task AddAsync(ContestEntry entry);
}