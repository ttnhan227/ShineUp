using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Client.Areas.Admin.Models.Dashboard
{
    public class DashboardDataResponse
    {
        [JsonPropertyName("stats")]
        public DashboardStatsDTO? Stats { get; set; }
        
        [JsonPropertyName("recentActivities")]
        public List<RecentActivityDTO>? RecentActivities { get; set; }
        
        [JsonPropertyName("applicationStatuses")]
        public List<ApplicationStatusDTO>? ApplicationStatuses { get; set; }
        
        [JsonPropertyName("opportunityTrends")]
        public List<OpportunityTrendDTO>? OpportunityTrends { get; set; }
        
        [JsonPropertyName("recentOpportunities")]
        public List<AdminOpportunityDTO>? RecentOpportunities { get; set; }
        
        [JsonPropertyName("activeContests")]
        public List<ContestDTO>? ActiveContests { get; set; }
    }

    public class DashboardStatsDTO
    {
        // User stats
        public int TotalUsers { get; set; }
        public int NewUsersThisMonth { get; set; }
        public int ActiveUsersToday { get; set; }
        public double UserGrowthPercentage { get; set; }
        
        // Opportunity stats
        public int TotalOpportunities { get; set; }
        public int OpenOpportunities { get; set; }
        public int ClosedOpportunities { get; set; }
        public int ApplicationsThisMonth { get; set; }
        public double ApplicationGrowthPercentage { get; set; }
        
        // Contest stats
        public int TotalContests { get; set; }
        public int ActiveContests { get; set; }
        public int TotalContestEntries { get; set; }
        public int ContestEntriesThisMonth { get; set; }
        
        // Content stats
        public int TotalPosts { get; set; }
        public int TotalCategories { get; set; }
        public int TotalComments { get; set; }
        public int TotalLikes { get; set; }
    }

    public class RecentActivityDTO
    {
        public int Id { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string? UserAvatar { get; set; }
    }

    public class ApplicationStatusDTO
    {
        public string Status { get; set; } = string.Empty;
        public int Count { get; set; }
        public double Percentage { get; set; }
    }

    public class OpportunityTrendDTO
    {
        public string Period { get; set; } = string.Empty;
        public int Opportunities { get; set; }
        public int Applications { get; set; }
    }

    public class AdminOpportunityDTO
    {
        public int? Id { get; set; }
        public string? Title { get; set; }
        public string? Location { get; set; }
        public bool? IsRemote { get; set; }
        public string? Type { get; set; }
        public string? Status { get; set; }
        public DateTime? ApplicationDeadline { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string? CategoryName { get; set; }
        public string? TalentArea { get; set; }
        public string? PostedBy { get; set; }
        public int? ApplicationCount { get; set; }
    }

    public class ContestDTO
    {
        public int? ContestID { get; set; }
        public string? Title { get; set; }
        public string? Description { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public bool? IsClosed { get; set; }
    }
}
