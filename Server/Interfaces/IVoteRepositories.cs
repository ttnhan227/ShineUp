using Server.Models;

namespace Server.Interfaces
{
    public interface IVoteRepositories
    {
        Task<bool> HasVotedAsync(int entryId, int userId);
        Task AddAsync(Vote vote);
        Task<List<object>> GetVoteResultsAsync(int contestId);
    }
}
