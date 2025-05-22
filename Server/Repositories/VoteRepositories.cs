using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Interfaces;
using Server.Models;

namespace Server.Repositories;

public class VoteRepositories : IVoteRepositories
{
    private readonly DatabaseContext _context;

    public VoteRepositories(DatabaseContext context)
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
            .ThenInclude(e => e.Video)
            .Include(v => v.ContestEntry.User)
            .Where(v => v.ContestEntry.ContestID == contestId)
            .GroupBy(v => v.EntryID)
            .Select(g => new
            {
                EntryID = g.Key,
                VoteCount = g.Count(),
                VideoTitle = g.First().ContestEntry.Video.Title,
                g.First().ContestEntry.Video.VideoURL,
                UserName = g.First().ContestEntry.User.Username
            })
            .Cast<object>()
            .ToListAsync();
    }
}