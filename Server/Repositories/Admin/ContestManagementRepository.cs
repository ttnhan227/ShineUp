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
        return await _context.Contests
            .Include(c => c.ContestEntries)
            .OrderByDescending(c => c.StartDate)
            .Select(c => new AdminContestDTO
            {
                ContestID = c.ContestID,
                Title = c.Title,
                Description = c.Description,
                StartDate = c.StartDate,
                EndDate = c.EndDate,
                ContestEntries = c.ContestEntries
            })
            .ToListAsync();
    }

    public async Task<AdminContestDTO?> GetContestByIdAsync(int id)
    {
        var contest = await _context.Contests
            .Include(c => c.ContestEntries)
            .FirstOrDefaultAsync(c => c.ContestID == id);

        if (contest == null)
            return null;

        return new AdminContestDTO
        {
            ContestID = contest.ContestID,
            Title = contest.Title,
            Description = contest.Description,
            StartDate = contest.StartDate,
            EndDate = contest.EndDate,
            ContestEntries = contest.ContestEntries
        };
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
        var entries = await _contestEntryRepositories.GetEntriesByContestAsync(contestId);
        
        return new ContestStatsDTO
        {
            TotalEntries = entries.Count,
            UniqueParticipants = entries.Select(e => e.UserID).Distinct().Count(),
            LastEntryDate = entries.Max(e => e.SubmissionDate)
        };
    }
}
