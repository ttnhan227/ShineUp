using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Server.DTOs.Admin;
using Server.Interfaces.Admin;
using System;
using System.Threading.Tasks;

namespace Server.Controllers.Admin;

[Authorize(Roles = "Admin")]
[Route("api/admin/dashboard")]
[ApiController]
public class DashboardController : ControllerBase
{
    private const int ADMIN_ROLE_ID = 2;
    private readonly ILogger<DashboardController> _logger;
    private readonly IDashboardRepository _repository;

    public DashboardController(
        IDashboardRepository repository,
        ILogger<DashboardController> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    private bool IsAdmin()
    {
        var roleIdClaim = User.FindFirst("RoleID");
        return roleIdClaim != null && int.TryParse(roleIdClaim.Value, out var roleId) && roleId == ADMIN_ROLE_ID;
    }

    /// <summary>
    /// Get complete dashboard data including statistics, recent activities, and trends
    /// </summary>
    /// <returns>Complete dashboard data</returns>
    [HttpGet]
    public async Task<ActionResult<DashboardDTO>> GetDashboardData()
    {
        if (!IsAdmin())
        {
            return Forbid();
        }

        try
        {
            var dashboardData = await _repository.GetDashboardDataAsync();
            return Ok(dashboardData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting dashboard data: {ex.Message}\n{ex.StackTrace}");
            return StatusCode(500, new { 
                message = "Internal server error while retrieving dashboard data",
                error = ex.Message,
                stackTrace = ex.StackTrace
            });
        }
    }

    /// <summary>
    /// Get dashboard statistics
    /// </summary>
    /// <returns>Dashboard statistics</returns>
    [HttpGet("stats")]
    public async Task<ActionResult<DashboardStatsDTO>> GetDashboardStats()
    {
        if (!IsAdmin())
        {
            return Forbid();
        }

        try
        {
            var stats = await _repository.GetDashboardStatsAsync();
            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting dashboard statistics: {ex.Message}\n{ex.StackTrace}");
            return StatusCode(500, new { 
                message = "Internal server error while retrieving dashboard statistics",
                error = ex.Message,
                stackTrace = ex.StackTrace
            });
        }
    }

    /// <summary>
    /// Get recent activities
    /// </summary>
    /// <param name="count">Number of recent activities to retrieve (default: 10)</param>
    /// <returns>List of recent activities</returns>
    [HttpGet("activities")]
    public async Task<ActionResult<IEnumerable<RecentActivityDTO>>> GetRecentActivities([FromQuery] int count = 10)
    {
        if (!IsAdmin())
        {
            return Forbid();
        }

        try
        {
            var activities = await _repository.GetRecentActivitiesAsync(count);
            return Ok(activities);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error getting recent activities: {ex.Message}");
            return StatusCode(500, new { message = "Internal server error while retrieving recent activities" });
        }
    }

    /// <summary>
    /// Get application status distribution
    /// </summary>
    /// <returns>List of application statuses with counts and percentages</returns>
    [HttpGet("application-statuses")]
    public async Task<ActionResult<IEnumerable<ApplicationStatusDTO>>> GetApplicationStatuses()
    {
        if (!IsAdmin())
        {
            return Forbid();
        }

        try
        {
            var statuses = await _repository.GetApplicationStatusDistributionAsync();
            return Ok(statuses);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error getting application status distribution: {ex.Message}");
            return StatusCode(500, new { message = "Internal server error while retrieving application status distribution" });
        }
    }

    /// <summary>
    /// Get opportunity trends over time
    /// </summary>
    /// <param name="startDate">Optional start date for filtering</param>
    /// <param name="endDate">Optional end date for filtering</param>
    /// <returns>List of opportunity trends by period</returns>
    [HttpGet("opportunity-trends")]
    public async Task<ActionResult<IEnumerable<OpportunityTrendDTO>>> GetOpportunityTrends(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        if (!IsAdmin())
        {
            return Forbid();
        }

        try
        {
            var trends = await _repository.GetOpportunityTrendsAsync(startDate, endDate);
            return Ok(trends);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error getting opportunity trends: {ex.Message}");
            return StatusCode(500, new { message = "Internal server error while retrieving opportunity trends" });
        }
    }

    /// <summary>
    /// Get user growth data for a specific period
    /// </summary>
    /// <param name="startDate">Start date</param>
    /// <param name="endDate">End date</param>
    /// <returns>User growth percentage</returns>
    [HttpGet("user-growth")]
    public async Task<ActionResult<double>> GetUserGrowth(
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate)
    {
        if (!IsAdmin())
        {
            return Forbid();
        }

        try
        {
            var growthPercentage = await _repository.CalculateUserGrowthPercentageAsync(startDate, endDate);
            return Ok(growthPercentage);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error calculating user growth: {ex.Message}");
            return StatusCode(500, new { message = "Internal server error while calculating user growth" });
        }
    }
}
