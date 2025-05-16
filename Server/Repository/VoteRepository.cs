
using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Models;
using Server.Repository;

namespace Server.Repositories
{
    public class VoteRepository : IVoteRepository
    {
        private readonly DatabaseContext _context;

        public VoteRepository(DatabaseContext context)
        {
            _context = context;
        }

        public async Task<bool> HasVotedAsync(int entryId, int userId)
        {
            return await _context.Votes.AnyAsync(v => v.EntryID == entryId && v.UserID == userId);
        }

        public async Task AddAsync(Vote vote)
        {
            _context.Votes.Add(vote);
            await _context.SaveChangesAsync();
        }

        public async Task<List<object>> GetVoteResultsAsync(int contestId)
        {
            return await _context.Votes
                .Include(v => v.ContestEntry)
                .Where(v => v.ContestEntry.ContestID == contestId)
                .GroupBy(v => v.EntryID)
                .Select(g => new
                {
                    EntryID = g.Key,
                    VoteCount = g.Count()
                })
                .Cast<object>()
                .ToListAsync();
        }
    }
}
