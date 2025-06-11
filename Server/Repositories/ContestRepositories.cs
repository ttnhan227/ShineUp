using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Interfaces;
using Server.Models;

namespace Server.Repositories;

public class ContestRepositories : IContestRepositories
{
    private readonly DatabaseContext _context;

    public ContestRepositories(DatabaseContext context)
    {
        _context = context;
    }

    public async Task<List<Contest>> GetAllAsync()
    {
        return await _context.Contests.ToListAsync();
    }

    public async Task<Contest?> GetByIdAsync(int id)
    {
        return await _context.Contests.FindAsync(id);
    }

    public async Task AddAsync(Contest contest)
    {
        _context.Contests.Add(contest);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Contest contest)
    {
        var existing = await _context.Contests.FindAsync(contest.ContestID);
        if (existing != null)
        {
            existing.Title = contest.Title;
            existing.Description = contest.Description;
            existing.StartDate = contest.StartDate;
            existing.EndDate = contest.EndDate;
            existing.IsClosed = contest.IsClosed;

            await _context.SaveChangesAsync();
        }
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