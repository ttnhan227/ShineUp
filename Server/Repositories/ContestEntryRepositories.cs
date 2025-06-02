using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Interfaces;
using Server.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Server.Repositories
{
    public class ContestEntryRepositories : IContestEntryRepositories
    {
        private readonly DatabaseContext _context;

        public ContestEntryRepositories(DatabaseContext context)
        {
            _context = context;
        }

        public async Task<bool> HasSubmittedAsync(int contestId, int userId)
        {
            return await _context.ContestEntries
                .AnyAsync(e => e.ContestID == contestId && e.UserID == userId);
        }

        public async Task<List<ContestEntry>> GetEntriesByContestAsync(int contestId)
        {
            return await _context.ContestEntries
                .Include(e => e.Video)
                .Include(e => e.Image)
                .Include(e => e.User)
                .Where(e => e.ContestID == contestId)
                .OrderByDescending(e => e.SubmissionDate)
                .ToListAsync();
        }

        public async Task<ContestEntry> GetByIdAsync(int entryId)
        {
            return await _context.ContestEntries
                .Include(e => e.Video)
                .Include(e => e.Image)
                .Include(e => e.User)
                .FirstOrDefaultAsync(e => e.EntryID == entryId);
        }

        public async Task AddAsync(ContestEntry entry)
        {
            _context.ContestEntries.Add(entry);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(ContestEntry entry)
        {
            _context.Entry(entry).State = EntityState.Modified;
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int entryId)
        {
            var entry = await _context.ContestEntries.FindAsync(entryId);
            if (entry != null)
            {
                _context.ContestEntries.Remove(entry);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<bool> IsVideoInUseAsync(string videoId)
        {
            return await _context.ContestEntries
                .AnyAsync(e => e.VideoID == videoId);
        }

        public async Task<bool> IsImageInUseAsync(string imageId)
        {
            return await _context.ContestEntries
                .AnyAsync(e => e.ImageID == imageId);
        }
    }
}