using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.DTOs;
using Server.DTOs.Admin;
using Server.Interfaces;
using Server.Interfaces.Admin;
using Server.Models;

namespace Server.Repositories.Admin;

public class ContestManagementRepository : IContestManagementRepository
{
    private readonly DatabaseContext _context;
    private readonly IContestRepositories _contestRepositories;
    private readonly IContestEntryRepositories _contestEntryRepositories;

    public ContestManagementRepository(
        DatabaseContext context,
        IContestRepositories contestRepositories,
        IContestEntryRepositories contestEntryRepositories)
    {
        _context = context;
        _contestRepositories = contestRepositories;
        _contestEntryRepositories = contestEntryRepositories;
    }

    public async Task<IEnumerable<AdminContestDTO>> GetAllContestsAsync()
    {
        // First, get all contests with their entries and users
        var contests = await _context.Contests
            .Include(c => c.ContestEntries)
                .ThenInclude(e => e.User)
            .OrderByDescending(c => c.StartDate)
            .ToListAsync();

        // Get all entry IDs to fetch their vote counts in a single query
        var entryIds = contests.SelectMany(c => c.ContestEntries).Select(e => e.EntryID).ToList();
        
        // Get vote counts for all entries in one query
        var voteCounts = await _context.Votes
            .Where(v => entryIds.Contains(v.EntryID))
            .GroupBy(v => v.EntryID)
            .Select(g => new { EntryID = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.EntryID, x => x.Count);

        // Map to DTOs and set vote counts
        return contests.Select(c => new AdminContestDTO
        {
            ContestID = c.ContestID,
            Title = c.Title,
            Description = c.Description,
            StartDate = c.StartDate,
            EndDate = c.EndDate,
            IsClosed = c.IsClosed,
            ContestEntries = c.ContestEntries.Select(e => new ContestEntryDTO
            {
                EntryID = e.EntryID,
                ContestID = e.ContestID,
                UserID = e.UserID,
                UserName = e.User?.Username ?? "Unknown",
                UserAvatar = e.User?.ProfileImageURL,
                Title = e.Title,
                Description = e.Description,
                SubmissionDate = e.SubmissionDate,
                MediaUrl = e.MediaUrl,
                MediaType = e.MediaType,
                VoteCount = voteCounts.TryGetValue(e.EntryID, out var count) ? count : 0,
                HasVoted = false,
                IsWinner = e.IsWinner
            }).ToList()
        });
    }

    public async Task<AdminContestDTO?> GetContestByIdAsync(int id)
    {
        // Get the contest with its entries and related data
        var contest = await _context.Contests
            .Include(c => c.ContestEntries)
                .ThenInclude(e => e.User)
            .Include(c => c.ContestEntries)
                .ThenInclude(e => e.Video)
            .Include(c => c.ContestEntries)
                .ThenInclude(e => e.Image)
            .FirstOrDefaultAsync(c => c.ContestID == id);

        if (contest == null)
            return null;

        // Get entry IDs to fetch their vote counts
        var entryIds = contest.ContestEntries.Select(e => e.EntryID).ToList();
        
        // Get vote counts for all entries in one query
        var voteCounts = await _context.Votes
            .Where(v => entryIds.Contains(v.EntryID))
            .GroupBy(v => v.EntryID)
            .Select(g => new { EntryID = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.EntryID, x => x.Count);

        var contestDto = new AdminContestDTO
        {
            ContestID = contest.ContestID,
            Title = contest.Title,
            Description = contest.Description,
            StartDate = contest.StartDate,
            EndDate = contest.EndDate,
            ContestEntries = contest.ContestEntries.Select(e => new ContestEntryDTO
            {
                EntryID = e.EntryID,
                ContestID = e.ContestID,
                UserID = e.UserID,
                UserName = e.User?.Username ?? "Unknown",
                UserAvatar = e.User?.ProfileImageURL,
                VideoID = e.VideoID,
                ImageID = e.ImageID,
                Title = e.Title,
                Description = e.Description,
                SubmissionDate = e.SubmissionDate,
                MediaUrl = !string.IsNullOrEmpty(e.VideoID) ? (e.Video?.VideoURL) : (e.Image?.ImageURL),
                MediaType = !string.IsNullOrEmpty(e.VideoID) ? "video" : "image",
                VoteCount = voteCounts.TryGetValue(e.EntryID, out var count) ? count : 0,
                HasVoted = false,
                IsWinner = e.IsWinner
            }).ToList()
        };

        return contestDto;
    }

    public async Task<AdminContestDTO> CreateContestAsync(AdminCreateContestDTO createContestDto, int createdBy)
    {
        var contest = new Contest
        {
            Title = createContestDto.Title,
            Description = createContestDto.Description,
            StartDate = createContestDto.StartDate,
            EndDate = createContestDto.EndDate
        };

        await _contestRepositories.AddAsync(contest);
        await _context.SaveChangesAsync();

        return new AdminContestDTO
        {
            ContestID = contest.ContestID,
            Title = contest.Title,
            Description = contest.Description,
            StartDate = contest.StartDate,
            EndDate = contest.EndDate
        };
    }


    public async Task<AdminContestDTO?> UpdateContestAsync(int id, AdminUpdateContestDTO updateContestDto)
    {
        var contest = await _contestRepositories.GetByIdAsync(id);
        if (contest == null)
            return null;

        contest.Title = updateContestDto.Title;
        contest.Description = updateContestDto.Description;
        contest.StartDate = updateContestDto.StartDate;
        contest.EndDate = updateContestDto.EndDate;

        await _contestRepositories.UpdateAsync(contest);
        await _context.SaveChangesAsync();

        // Refresh the contest to get the latest data including navigation properties
        return await GetContestByIdAsync(id);
    }

    public async Task<bool> DeleteContestAsync(int id)
    {
        var contest = await _contestRepositories.GetByIdAsync(id);
        if (contest == null)
            return false;

        // First delete all entries for this contest
        var entries = await _contestEntryRepositories.GetEntriesByContestAsync(id);
        foreach (var entry in entries)
        {
            await _contestEntryRepositories.DeleteAsync(entry.EntryID);
        }

        // Then delete the contest
        await _contestRepositories.DeleteAsync(id);
        return true;
    }

    public async Task<IEnumerable<ContestEntry>> GetContestEntriesAsync(int contestId)
    {
        return await _contestEntryRepositories.GetEntriesByContestAsync(contestId);
    }

    public async Task<ContestEntry?> GetContestEntryByIdAsync(int entryId)
    {
        return await _contestEntryRepositories.GetByIdAsync(entryId);
    }

    public async Task<bool> DeleteContestEntryAsync(int entryId)
    {
        var entry = await _contestEntryRepositories.GetByIdAsync(entryId);
        if (entry == null)
            return false;

        await _contestEntryRepositories.DeleteAsync(entryId);
        return true;
    }

    public async Task<ContestStatsDTO> GetContestStatsAsync(int contestId)
    {
        var contest = await _context.Contests
            .Include(c => c.ContestEntries)
            .FirstOrDefaultAsync(c => c.ContestID == contestId);

        if (contest == null)
            return null;

        return new ContestStatsDTO
        {
            TotalEntries = contest.ContestEntries.Count,
            UniqueParticipants = contest.ContestEntries.Select(e => e.UserID).Distinct().Count(),
            LastEntryDate = contest.ContestEntries.Any() ? contest.ContestEntries.Max(e => e.SubmissionDate) : null
        };
    }
    
    public async Task<bool> DeclareWinnerAsync(int entryId)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        
        try
        {
            // Get the entry with its contest
            var entry = await _context.ContestEntries
                .Include(e => e.Contest)
                .FirstOrDefaultAsync(e => e.EntryID == entryId);
                
            if (entry == null || entry.Contest == null)
                return false;
                
            // Mark the contest as closed
            entry.Contest.IsClosed = true;
            
            // Mark this entry as winner
            entry.IsWinner = true;
            
            // Save changes
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            
            return true;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw; // Re-throw to be handled by the controller
        }
    }
}
