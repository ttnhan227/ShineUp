using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Models;

namespace Server.Repositories
{
    public class ContestRepository : IContestRepository
    {
        private readonly DatabaseContext _context;

        public ContestRepository(DatabaseContext context)
        {
            _context = context;
        }

        public async Task<List<Contest>> GetAllAsync() => await _context.Contests.ToListAsync();
        public async Task<Contest?> GetByIdAsync(int id) => await _context.Contests.FindAsync(id);

        public async Task AddAsync(Contest contest)
        {
            _context.Contests.Add(contest);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Contest contest)
        {
            _context.Contests.Update(contest);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var contest = await _context.Contests.FindAsync(id);
            if (contest != null)
            {
                _context.Contests.Remove(contest);
                await _context.SaveChangesAsync();
            }
        }
    }

}
