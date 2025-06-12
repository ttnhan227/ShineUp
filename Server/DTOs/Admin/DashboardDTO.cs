using System;
using System.Collections.Generic;

namespace Server.DTOs.Admin;

public class DashboardStatsDTO
{
    // User Statistics
    public int TotalUsers { get; set; }
    public int NewUsersThisMonth { get; set; }
    public int ActiveUsersToday { get; set; }
    public double UserGrowthPercentage { get; set; }
    
    // Opportunity Statistics
    public int TotalOpportunities { get; set; }
    public int OpenOpportunities { get; set; }
    public int ClosedOpportunities { get; set; }
    public int ApplicationsThisMonth { get; set; }
    public double ApplicationGrowthPercentage { get; set; }
    
    // Contest Statistics
    public int TotalContests { get; set; }
    public int ActiveContests { get; set; }
    public int TotalContestEntries { get; set; }
    public int ContestEntriesThisMonth { get; set; }
    
    // Content Statistics
    public int TotalPosts { get; set; }
    public int TotalCategories { get; set; }
    public int TotalComments { get; set; }
    public int TotalLikes { get; set; }
}

public class RecentActivityDTO
{
    public int Id { get; set; }
    public string Type { get; set; } = string.Empty; // "User", "Opportunity", "Contest", "Application"
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string UserAvatar { get; set; } = string.Empty;
}

public class ApplicationStatusDTO
{
    public string Status { get; set; } = string.Empty;
    public int Count { get; set; }
    public double Percentage { get; set; }
}

public class OpportunityTrendDTO
{
    public string Period { get; set; } = string.Empty; // e.g., "Jan 2023", "Week 1", etc.
    public int Opportunities { get; set; }
    public int Applications { get; set; }
}

public class DashboardDTO
{
    public DashboardStatsDTO Stats { get; set; } = new();
    public List<RecentActivityDTO> RecentActivities { get; set; } = new();
    public List<ApplicationStatusDTO> ApplicationStatuses { get; set; } = new();
    public List<OpportunityTrendDTO> OpportunityTrends { get; set; } = new();
    public List<AdminOpportunityDTO> RecentOpportunities { get; set; } = new();
    public List<ContestDTO> ActiveContests { get; set; } = new();
}

// Extension method for calculating percentage
public static class DashboardExtensions
{
    public static double CalculatePercentage(this int value, int total)
    {
        return total > 0 ? Math.Round((double)value / total * 100, 2) : 0;
    }
}
