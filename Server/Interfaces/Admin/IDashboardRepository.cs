using System.Collections.Generic;
using System.Threading.Tasks;

namespace Server.Interfaces.Admin;

public interface IDashboardRepository
{
    Task<int> GetTotalUsersAsync();
    Task<int> GetTotalOpportunitiesAsync();
    Task<int> GetTotalApplicationsAsync();
    Task<int> GetTotalPostsAsync();
    Task<IEnumerable<dynamic>> GetRecentUsersAsync(int count);
    Task<IEnumerable<dynamic>> GetRecentApplicationsAsync(int count);
    Task<object> GetOpportunityStatsAsync();
    Task<object> GetApplicationStatusDistributionAsync();
}
