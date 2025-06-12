using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.DTOs;
using Server.DTOs.Admin;
using Server.Interfaces.Admin;
using Server.Models; // For ApplicationStatus and other enums
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Server.Repositories.Admin;

public class DashboardRepository : IDashboardRepository
{
    private readonly DatabaseContext _context;
    private readonly ILogger<DashboardRepository> _logger;

    public DashboardRepository(DatabaseContext context, ILogger<DashboardRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<DashboardDTO> GetDashboardDataAsync()
    {
        var now = DateTime.UtcNow;
        var startOfMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var startOfDay = new DateTime(now.Year, now.Month, now.Day, 0, 0, 0, DateTimeKind.Utc);
        var lastMonthStart = startOfMonth.AddMonths(-1);
        var lastYearStart = now.AddYears(-1);
        
        // Ensure all dates are in UTC
        startOfMonth = DateTime.SpecifyKind(startOfMonth, DateTimeKind.Utc);
        startOfDay = DateTime.SpecifyKind(startOfDay, DateTimeKind.Utc);
        lastMonthStart = DateTime.SpecifyKind(lastMonthStart, DateTimeKind.Utc);
        lastYearStart = DateTime.SpecifyKind(lastYearStart, DateTimeKind.Utc);

        var stats = await GetDashboardStatsAsync();
        var recentActivities = await GetRecentActivitiesAsync(10);
        var applicationStatuses = await GetApplicationStatusDistributionAsync();
        var opportunityTrends = await GetOpportunityTrendsAsync(lastYearStart, now);
        
        // Get recent opportunities (last 5)
        var recentOpportunities = await _context.TalentOpportunities
            .Where(o => o.CreatedAt >= now.AddMonths(-3)) // Last 3 months
            .OrderByDescending(o => o.CreatedAt)
            .Take(5)
            .Select(o => new AdminOpportunityDTO
            {
                Id = o.Id,
                Title = o.Title,
                Location = o.Location,
                IsRemote = o.IsRemote,
                Type = o.Type.ToString(),
                Status = o.Status.ToString(),
                ApplicationDeadline = o.ApplicationDeadline,
                CreatedAt = o.CreatedAt,
                CategoryName = o.CategoryId != null ? _context.Categories
                    .Where(c => c.CategoryID == o.CategoryId)
                    .Select(c => c.CategoryName)
                    .FirstOrDefault() : "Uncategorized",
                TalentArea = o.TalentArea ?? "General",
                PostedBy = _context.Users
                    .Where(u => u.UserID == o.PostedByUserId)
                    .Select(u => u.Username)
                    .FirstOrDefault() ?? "Unknown",
                ApplicationCount = _context.OpportunityApplications
                    .Count(a => a.TalentOpportunityID == o.Id)
            })
            .ToListAsync();

        // Get active contests
        var activeContests = await _context.Contests
            .Where(c => c.StartDate <= now && c.EndDate >= now)
            .OrderBy(c => c.EndDate)
            .Take(5)
            .Select(c => new ContestDTO
            {
                ContestID = c.ContestID,
                Title = c.Title,
                Description = c.Description,
                StartDate = c.StartDate,
                EndDate = c.EndDate,
                IsClosed = c.IsClosed 
            })
            .ToListAsync();

        return new DashboardDTO
        {
            Stats = stats,
            RecentActivities = recentActivities.ToList(),
            ApplicationStatuses = applicationStatuses.ToList(),
            OpportunityTrends = opportunityTrends.ToList(),
            RecentOpportunities = recentOpportunities,
            ActiveContests = activeContests
        };
    }

    public async Task<DashboardStatsDTO> GetDashboardStatsAsync()
    {
        var now = DateTime.UtcNow;
        var startOfMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var startOfDay = new DateTime(now.Year, now.Month, now.Day, 0, 0, 0, DateTimeKind.Utc);
        var lastMonthStart = startOfMonth.AddMonths(-1);
        
        // Ensure all dates are in UTC
        startOfMonth = DateTime.SpecifyKind(startOfMonth, DateTimeKind.Utc);
        startOfDay = DateTime.SpecifyKind(startOfDay, DateTimeKind.Utc);
        lastMonthStart = DateTime.SpecifyKind(lastMonthStart, DateTimeKind.Utc);

        // User statistics
        var totalUsers = await GetTotalUsersCountAsync();
        var newUsersThisMonth = await GetNewUsersCountAsync(startOfMonth, now);
        var activeUsersToday = await GetActiveUsersCountAsync(startOfDay);
        var userGrowthPercentage = await CalculateUserGrowthPercentageAsync(lastMonthStart, startOfMonth);

        // Opportunity statistics
        var totalOpportunities = await GetTotalOpportunitiesCountAsync();
        var openOpportunities = await GetOpenOpportunitiesCountAsync();
        var closedOpportunities = await GetClosedOpportunitiesCountAsync();
        var applicationsThisMonth = await GetApplicationsCountAsync(startOfMonth, now);
        var applicationGrowthPercentage = await CalculateApplicationGrowthPercentageAsync(lastMonthStart, startOfMonth);

        // Contest statistics
        var totalContests = await GetTotalContestsCountAsync();
        var activeContests = await GetActiveContestsCountAsync();
        var totalContestEntries = await GetTotalContestEntriesCountAsync();
        var contestEntriesThisMonth = await GetContestEntriesCountAsync(startOfMonth, now);

        // Content statistics
        var totalPosts = await GetTotalPostsCountAsync();
        var totalCategories = await GetTotalCategoriesCountAsync();
        var totalComments = await GetTotalCommentsCountAsync();
        var totalLikes = await GetTotalLikesCountAsync();

        return new DashboardStatsDTO
        {
            // User stats
            TotalUsers = totalUsers,
            NewUsersThisMonth = newUsersThisMonth,
            ActiveUsersToday = activeUsersToday,
            UserGrowthPercentage = userGrowthPercentage,
            
            // Opportunity stats
            TotalOpportunities = totalOpportunities,
            OpenOpportunities = openOpportunities,
            ClosedOpportunities = closedOpportunities,
            ApplicationsThisMonth = applicationsThisMonth,
            ApplicationGrowthPercentage = applicationGrowthPercentage,
            
            // Contest stats
            TotalContests = totalContests,
            ActiveContests = activeContests,
            TotalContestEntries = totalContestEntries,
            ContestEntriesThisMonth = contestEntriesThisMonth,
            
            // Content stats
            TotalPosts = totalPosts,
            TotalCategories = totalCategories,
            TotalComments = totalComments,
            TotalLikes = totalLikes
        };
    }

    public async Task<IEnumerable<RecentActivityDTO>> GetRecentActivitiesAsync(int count = 10)
    {
        var now = DateTime.UtcNow;
        var thirtyDaysAgo = now.AddDays(-30).ToUniversalTime();
        
        // Ensure all dates are in UTC
        now = DateTime.SpecifyKind(now, DateTimeKind.Utc);
        thirtyDaysAgo = DateTime.SpecifyKind(thirtyDaysAgo, DateTimeKind.Utc);

        // Get recent user registrations
        var newUsers = await _context.Users
            .Where(u => u.CreatedAt >= thirtyDaysAgo)
            .OrderByDescending(u => u.CreatedAt)
            .Take(count / 2) // Half of the activities
            .Select(u => new RecentActivityDTO
            {
                Id = u.UserID,
                Type = "User",
                Title = "New User Registration",
                Description = $"{u.Username} joined the platform",
                Timestamp = u.CreatedAt,
                UserName = u.Username,
                UserAvatar = u.ProfileImageURL
            })
            .ToListAsync();

        // Get recent opportunities (the other half)
        var recentOpportunities = await _context.TalentOpportunities
            .Where(o => o.CreatedAt >= thirtyDaysAgo)
            .OrderByDescending(o => o.CreatedAt)
            .Take(count / 2)
            .Select(o => new RecentActivityDTO
            {
                Id = o.Id,
                Type = "Opportunity",
                Title = "New Opportunity Created",
                Description = $"New opportunity: {o.Title}",
                Timestamp = o.CreatedAt,
                UserName = _context.Users
                    .Where(u => u.UserID == o.PostedByUserId)
                    .Select(u => u.Username)
                    .FirstOrDefault() ?? "Unknown",
                UserAvatar = _context.Users
                    .Where(u => u.UserID == o.PostedByUserId)
                    .Select(u => u.ProfileImageURL)
                    .FirstOrDefault()
            })
            .ToListAsync();

        // Combine and order all activities
        var allActivities = newUsers.Concat(recentOpportunities)
            .OrderByDescending(a => a.Timestamp)
            .Take(count)
            .ToList();

        return allActivities;
    }

    public async Task<IEnumerable<ApplicationStatusDTO>> GetApplicationStatusDistributionAsync()
    {
        var statusGroups = await _context.OpportunityApplications
            .GroupBy(a => a.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync();

        var total = statusGroups.Sum(g => g.Count);

        return statusGroups.Select(g => new ApplicationStatusDTO
        {
            Status = g.Status.ToString(),
            Count = g.Count,
            Percentage = total > 0 ? Math.Round((double)g.Count / total * 100, 2) : 0
        }).ToList();
    }

    public async Task<IEnumerable<OpportunityTrendDTO>> GetOpportunityTrendsAsync(DateTime? startDate = null, DateTime? endDate = null)
    {
        // Ensure all dates are in UTC
        if (startDate.HasValue)
            startDate = DateTime.SpecifyKind(startDate.Value, DateTimeKind.Utc);
            
        if (endDate.HasValue)
            endDate = DateTime.SpecifyKind(endDate.Value, DateTimeKind.Utc);
        
        var query = _context.TalentOpportunities.AsQueryable();
        
        if (startDate.HasValue)
            query = query.Where(o => o.CreatedAt >= startDate.Value);
            
        if (endDate.HasValue)
            query = query.Where(o => o.CreatedAt <= endDate.Value);

        // Group by month for the trend
        var monthlyTrends = await query
            .GroupBy(o => new { o.CreatedAt.Year, o.CreatedAt.Month })
            .OrderBy(g => g.Key.Year)
            .ThenBy(g => g.Key.Month)
            .Select(g => new OpportunityTrendDTO
            {
                Period = $"{new DateTime(g.Key.Year, g.Key.Month, 1):MMM yyyy}",
                Opportunities = g.Count(),
                Applications = g.Sum(o => _context.OpportunityApplications.Count(a => a.TalentOpportunityID == o.Id))
            })
            .ToListAsync();

        return monthlyTrends;
    }

    // Individual statistics methods
    public async Task<int> GetTotalUsersCountAsync() => 
        await _context.Users.CountAsync();

    public async Task<int> GetNewUsersCountAsync(DateTime startDate, DateTime endDate)
    {
        // Ensure dates are in UTC
        var utcStartDate = DateTime.SpecifyKind(startDate, DateTimeKind.Utc);
        var utcEndDate = DateTime.SpecifyKind(endDate, DateTimeKind.Utc);
        
        return await _context.Users
            .CountAsync(u => u.CreatedAt >= utcStartDate && u.CreatedAt <= utcEndDate);
    }

    public async Task<int> GetActiveUsersCountAsync(DateTime date)
    {
        // Ensure date is in UTC
        var utcDate = DateTime.SpecifyKind(date, DateTimeKind.Utc);
        
        // Using CreatedAt as a fallback since we don't have a LastLoginDate field
        return await _context.Users
            .CountAsync(u => u.CreatedAt >= utcDate);
    }

    public async Task<int> GetTotalOpportunitiesCountAsync() => 
        await _context.TalentOpportunities.CountAsync();

    public async Task<int> GetOpenOpportunitiesCountAsync() => 
        await _context.TalentOpportunities.CountAsync(o => o.Status == OpportunityStatus.Open);

    public async Task<int> GetClosedOpportunitiesCountAsync() => 
        await _context.TalentOpportunities.CountAsync(o => o.Status == OpportunityStatus.Closed);

    public async Task<int> GetApplicationsCountAsync(DateTime? startDate = null, DateTime? endDate = null)
    {
        var query = _context.OpportunityApplications.AsQueryable();
        
        if (startDate.HasValue)
        {
            var utcStartDate = DateTime.SpecifyKind(startDate.Value, DateTimeKind.Utc);
            query = query.Where(a => a.AppliedAt >= utcStartDate);
        }
            
        if (endDate.HasValue)
        {
            var utcEndDate = DateTime.SpecifyKind(endDate.Value, DateTimeKind.Utc);
            query = query.Where(a => a.AppliedAt <= utcEndDate);
        }
            
        return await query.CountAsync();
    }

    public async Task<int> GetTotalContestsCountAsync() => 
        await _context.Contests.CountAsync();

    public async Task<int> GetActiveContestsCountAsync()
    {
        var now = DateTime.UtcNow;
        return await _context.Contests
            .CountAsync(c => c.StartDate <= now && c.EndDate >= now);
    }

    public async Task<int> GetTotalContestEntriesCountAsync() => 
        await _context.ContestEntries.CountAsync();

    public async Task<int> GetContestEntriesCountAsync(DateTime? startDate = null, DateTime? endDate = null)
    {
        var query = _context.ContestEntries.AsQueryable();
        
        if (startDate.HasValue)
        {
            var utcStartDate = DateTime.SpecifyKind(startDate.Value, DateTimeKind.Utc);
            query = query.Where(e => e.SubmissionDate >= utcStartDate);
        }
            
        if (endDate.HasValue)
        {
            var utcEndDate = DateTime.SpecifyKind(endDate.Value, DateTimeKind.Utc);
            query = query.Where(e => e.SubmissionDate <= utcEndDate);
        }
            
        return await query.CountAsync();
    }

    public async Task<int> GetTotalPostsCountAsync() => 
        await _context.Posts.CountAsync();

    public async Task<int> GetTotalCategoriesCountAsync() => 
        await _context.Categories.CountAsync();

    public async Task<int> GetTotalCommentsCountAsync() => 
        await _context.Comments.CountAsync();

    public async Task<int> GetTotalLikesCountAsync() => 
        await _context.Likes.CountAsync();

    public async Task<double> CalculateUserGrowthPercentageAsync(DateTime startDate, DateTime endDate)
    {
        var startCount = await _context.Users
            .CountAsync(u => u.CreatedAt < startDate);
            
        var endCount = await _context.Users
            .CountAsync(u => u.CreatedAt < endDate);
            
        if (startCount == 0) return endCount > 0 ? 100 : 0;
        
        return Math.Round((double)(endCount - startCount) / startCount * 100, 2);
    }

    public async Task<double> CalculateApplicationGrowthPercentageAsync(DateTime startDate, DateTime endDate)
    {
        var startCount = await _context.OpportunityApplications
            .CountAsync(a => a.AppliedAt < startDate);
            
        var endCount = await _context.OpportunityApplications
            .CountAsync(a => a.AppliedAt < endDate);
            
        if (startCount == 0) return endCount > 0 ? 100 : 0;
        
        return Math.Round((double)(endCount - startCount) / startCount * 100, 2);
    }
}
