using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Server.Interfaces.Admin;

namespace Server.Controllers.Admin;

[Authorize(Roles = "Admin")]
[Route("api/admin/dashboard")]
[ApiController]
public class DashboardController : ControllerBase
{
    private readonly IDashboardRepository _dashboardRepository;

    public DashboardController(IDashboardRepository dashboardRepository)
    {
        _dashboardRepository = dashboardRepository;
    }

    [HttpGet]
    public async Task<IActionResult> GetDashboardStats([FromQuery] int? timeRange = null)
    {
        try
        {
            var stats = new
            {
                TotalUsers = await _dashboardRepository.GetTotalUsersAsync(),
                TotalOpportunities = await _dashboardRepository.GetTotalOpportunitiesAsync(),
                TotalApplications = await _dashboardRepository.GetTotalApplicationsAsync(),
                TotalPosts = await _dashboardRepository.GetTotalPostsAsync(),
                RecentUsers = await _dashboardRepository.GetRecentUsersAsync(5),
                RecentApplications = await _dashboardRepository.GetRecentApplicationsAsync(5),
                OpportunityStats = await _dashboardRepository.GetOpportunityStatsAsync(),
                ApplicationStatus = await _dashboardRepository.GetApplicationStatusDistributionAsync()
            };
            
            return Ok(stats);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error retrieving dashboard statistics" });
        }
    }
}
