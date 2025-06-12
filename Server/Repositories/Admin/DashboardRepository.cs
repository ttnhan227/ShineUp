using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Interfaces.Admin;
using Server.Models;

namespace Server.Repositories.Admin;

public class DashboardRepository : IDashboardRepository
{
    private readonly DatabaseContext _context;

    public DashboardRepository(DatabaseContext context)
    {
        _context = context;
    }

    public async Task<int> GetTotalUsersAsync()
    {
        return await _context.Users.CountAsync();
    }

    public async Task<int> GetTotalOpportunitiesAsync()
    {
        return await _context.TalentOpportunities.CountAsync();
    }

    public async Task<int> GetTotalApplicationsAsync()
    {
        return await _context.OpportunityApplications.CountAsync();
    }

    public async Task<int> GetTotalPostsAsync()
    {
        return await _context.Posts.CountAsync();
    }

    public async Task<IEnumerable<dynamic>> GetRecentUsersAsync(int count)
    {
        return await _context.Users
            .OrderByDescending(u => u.CreatedAt)
            .Take(count)
            .Select(u => new 
            { 
                Id = u.UserID, 
                u.Username, 
                u.Email, 
                u.CreatedAt,
                ProfileImageUrl = u.ProfileImageURL
            })
            .ToListAsync();
    }

    public async Task<IEnumerable<dynamic>> GetRecentApplicationsAsync(int count)
    {
        return await _context.OpportunityApplications
            .Include(a => a.TalentOpportunity)
            .Include(a => a.User)
            .OrderByDescending(a => a.AppliedAt)
            .Take(count)
            .Select(a => new 
            { 
Id = a.ApplicationID,
                OpportunityTitle = a.TalentOpportunity.Title,
                UserName = a.User.Username,
                Status = a.Status,
                AppliedAt = a.AppliedAt
            })
            .ToListAsync();
    }

    public async Task<object> GetOpportunityStatsAsync()
    {
        var lastMonth = DateTime.UtcNow.AddMonths(-1);
        
        var stats = await _context.TalentOpportunities
            .GroupBy(o => o.CreatedAt.Month)
            .Select(g => new 
            { 
                Month = g.Key,
                Count = g.Count()
            })
            .OrderBy(s => s.Month)
            .ToListAsync();

        return new 
        {
            Labels = stats.Select(s => new DateTime(DateTime.UtcNow.Year, s.Month, 1).ToString("MMM")),
            Data = stats.Select(s => s.Count)
        };
    }


    public async Task<object> GetApplicationStatusDistributionAsync()
    {
        return await _context.OpportunityApplications
            .GroupBy(a => a.Status)
            .Select(g => new 
            { 
                Status = g.Key.ToString(),
                Count = g.Count()
            })
            .ToListAsync();
    }
}
