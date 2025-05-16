using Server.Models;

namespace Server.Interfaces
{
    public interface IVoteRepository
    {
        Task<bool> HasVotedAsync(int entryId, int userId);
        Task AddAsync(Vote vote);
        Task<List<object>> GetVoteResultsAsync(int contestId);
    }
}
