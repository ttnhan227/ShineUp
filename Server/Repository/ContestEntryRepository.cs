using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Models;
using Server.Repository;

namespace Server.Repositories
{
    public class ContestEntryRepository : IContestEntryRepository
    {
        private readonly DatabaseContext _context;

        public ContestEntryRepository(DatabaseContext context)
        {
            _context = context;
        }

        public async Task<bool> HasSubmittedAsync(int contestId, int userId)
        {
            return await _context.ContestEntries
                .AnyAsync(e => e.ContestID == contestId && e.UserID == userId);
        }

        public async Task<List<object>> GetEntriesByContestAsync(int contestId)
        {
            return await _context.ContestEntries
                .Include(e => e.Video)
                .Include(e => e.User)
                .Where(e => e.ContestID == contestId)
                .Select(e => new
                {
                    e.EntryID,
                    e.ContestID,
                    videoTitle = e.Video.Title,
                    videoURL = e.Video.VideoURL,
                    userName = e.User.Username,
                    e.SubmissionDate
                })
                .Cast<object>()
                .ToListAsync();
        }

        public async Task AddAsync(ContestEntry entry)
        {
            _context.ContestEntries.Add(entry);
            await _context.SaveChangesAsync();
        }
    }
}
