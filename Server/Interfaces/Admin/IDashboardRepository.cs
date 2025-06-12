using Server.DTOs.Admin;
using System.Threading.Tasks;

namespace Server.Interfaces.Admin;

public interface IDashboardRepository
{
    // Main dashboard data
    Task<DashboardDTO> GetDashboardDataAsync();
    
    // Individual data methods for partial updates
    Task<DashboardStatsDTO> GetDashboardStatsAsync();
    Task<IEnumerable<RecentActivityDTO>> GetRecentActivitiesAsync(int count = 10);
    Task<IEnumerable<ApplicationStatusDTO>> GetApplicationStatusDistributionAsync();
    Task<IEnumerable<OpportunityTrendDTO>> GetOpportunityTrendsAsync(DateTime? startDate = null, DateTime? endDate = null);
    
    // Additional detailed stats
    Task<int> GetTotalUsersCountAsync();
    Task<int> GetNewUsersCountAsync(DateTime startDate, DateTime endDate);
    Task<int> GetActiveUsersCountAsync(DateTime date);
    
    Task<int> GetTotalOpportunitiesCountAsync();
    Task<int> GetOpenOpportunitiesCountAsync();
    Task<int> GetClosedOpportunitiesCountAsync();
    Task<int> GetApplicationsCountAsync(DateTime? startDate = null, DateTime? endDate = null);
    
    Task<int> GetTotalContestsCountAsync();
    Task<int> GetActiveContestsCountAsync();
    Task<int> GetTotalContestEntriesCountAsync();
    Task<int> GetContestEntriesCountAsync(DateTime? startDate = null, DateTime? endDate = null);
    
    Task<int> GetTotalPostsCountAsync();
    Task<int> GetTotalCategoriesCountAsync();
    Task<int> GetTotalCommentsCountAsync();
    Task<int> GetTotalLikesCountAsync();
    
    // User growth percentage calculation
    Task<double> CalculateUserGrowthPercentageAsync(DateTime startDate, DateTime endDate);
    
    // Application growth percentage calculation
    Task<double> CalculateApplicationGrowthPercentageAsync(DateTime startDate, DateTime endDate);
}
