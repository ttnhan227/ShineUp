using Server.Models;

namespace Server.Interfaces;

public interface IContestEntryRepositories
{
    Task<bool> HasSubmittedAsync(int contestId, int userId);
    Task<List<ContestEntry>> GetEntriesByContestAsync(int contestId);
    Task<ContestEntry> GetByIdAsync(int entryId);
    Task AddAsync(ContestEntry entry);
    Task UpdateAsync(ContestEntry entry);
    Task DeleteAsync(int entryId);

    // Media specific methods
    Task<bool> IsVideoInUseAsync(string videoId);
    Task<bool> IsImageInUseAsync(string imageId);
}