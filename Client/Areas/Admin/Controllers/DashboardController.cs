using System.Net.Http.Headers;
using System.Text.Json;
using Client.Areas.Admin.Models.Dashboard;
using Client.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ContestViewModel = Client.Areas.Admin.Models.ContestViewModel;

namespace Client.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class DashboardController : Controller
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<DashboardController> _logger;

    public DashboardController(
        IHttpClientFactory httpClientFactory,
        ILogger<DashboardController> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        var token = User.FindFirst("JWT")?.Value;
        if (string.IsNullOrEmpty(token))
        {
            _logger.LogWarning("User is not authenticated");
            return Unauthorized();
        }

        var client = _httpClientFactory.CreateClient("API");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        // Create the dashboard view model with default values
        var viewModel = new IndexViewModel
        {
            Stats = new StatsViewModel(),
            RecentActivities = new List<RecentActivityViewModel>(),
            ApplicationStatuses = new List<ApplicationStatusViewModel>(),
            OpportunityTrends = new List<OpportunityTrendViewModel>(),
            RecentOpportunities = new List<RecentOpportunityViewModel>(),
            ActiveContests = new List<ContestViewModel>()
        };

        try
        {
            // Get dashboard data
            var response = await client.GetAsync("api/admin/dashboard");
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError($"Failed to fetch dashboard data. Status code: {response.StatusCode}");
                return View(viewModel); // Return view with empty data
            }

            var json = await response.Content.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(json))
            {
                _logger.LogWarning("Received empty response from dashboard API");
                return View(viewModel);
            }

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            
            var dashboardData = JsonSerializer.Deserialize<DashboardDataResponse>(json, options);
            if (dashboardData == null)
            {
                _logger.LogWarning("Deserialized dashboard data is null");
                return View(viewModel);
            }

                    // Map stats
                    viewModel.Stats = new StatsViewModel
                    {
                        // User stats
                        TotalUsers = dashboardData.Stats?.TotalUsers ?? 0,
                        NewUsersThisMonth = dashboardData.Stats?.NewUsersThisMonth ?? 0,
                        ActiveUsersToday = dashboardData.Stats?.ActiveUsersToday ?? 0,
                        UserGrowthPercentage = dashboardData.Stats?.UserGrowthPercentage ?? 0,
                        
                        // Opportunity stats
                        TotalOpportunities = dashboardData.Stats?.TotalOpportunities ?? 0,
                        OpenOpportunities = dashboardData.Stats?.OpenOpportunities ?? 0,
                        ClosedOpportunities = dashboardData.Stats?.ClosedOpportunities ?? 0,
                        ApplicationsThisMonth = dashboardData.Stats?.ApplicationsThisMonth ?? 0,
                        ApplicationGrowthPercentage = dashboardData.Stats?.ApplicationGrowthPercentage ?? 0,
                        
                        // Contest stats
                        TotalContests = dashboardData.Stats?.TotalContests ?? 0,
                        ActiveContests = dashboardData.Stats?.ActiveContests ?? 0,
                        TotalContestEntries = dashboardData.Stats?.TotalContestEntries ?? 0,
                        ContestEntriesThisMonth = dashboardData.Stats?.ContestEntriesThisMonth ?? 0,
                        
                        // Content stats
                        TotalPosts = dashboardData.Stats?.TotalPosts ?? 0,
                        TotalCategories = dashboardData.Stats?.TotalCategories ?? 0,
                        TotalComments = dashboardData.Stats?.TotalComments ?? 0,
                        TotalLikes = dashboardData.Stats?.TotalLikes ?? 0
                    };

                    // Map recent activities
                    if (dashboardData.RecentActivities != null)
                    {
                        viewModel.RecentActivities = new List<RecentActivityViewModel>();
                        foreach (var activity in dashboardData.RecentActivities)
                        {
                            if (activity != null)
                            {
                                viewModel.RecentActivities.Add(new RecentActivityViewModel
                                {
                                    Id = activity.Id,
                                    Type = activity.Type ?? string.Empty,
                                    Title = activity.Title ?? string.Empty,
                                    Description = activity.Description ?? string.Empty,
                                    Timestamp = activity.Timestamp,
                                    UserName = activity.UserName ?? "Unknown User",
                                    UserAvatar = activity.UserAvatar
                                });
                            }
                        }
                    }

                    // Map application statuses
                    if (dashboardData.ApplicationStatuses != null)
                    {
                        viewModel.ApplicationStatuses = new List<ApplicationStatusViewModel>();
                        foreach (var status in dashboardData.ApplicationStatuses)
                        {
                            if (status != null)
                            {
                                viewModel.ApplicationStatuses.Add(new ApplicationStatusViewModel
                                {
                                    Status = status.Status ?? "Unknown",
                                    Count = status.Count,
                                    Percentage = status.Percentage
                                });
                            }
                        }
                    }

                    // Map opportunity trends
                    if (dashboardData.OpportunityTrends != null)
                    {
                        viewModel.OpportunityTrends = new List<OpportunityTrendViewModel>();
                        foreach (var trend in dashboardData.OpportunityTrends)
                        {
                            if (trend != null)
                            {
                                viewModel.OpportunityTrends.Add(new OpportunityTrendViewModel
                                {
                                    Period = trend.Period ?? string.Empty,
                                    Opportunities = trend.Opportunities,
                                    Applications = trend.Applications
                                });
                            }
                        }
                    }

                    // Map recent opportunities
                    if (dashboardData.RecentOpportunities != null)
                    {
                        viewModel.RecentOpportunities = new List<RecentOpportunityViewModel>();
                        foreach (var opp in dashboardData.RecentOpportunities)
                        {
                            if (opp != null)
                            {
                                viewModel.RecentOpportunities.Add(new RecentOpportunityViewModel
                                {
                                    Id = opp.Id ?? 0,
                                    Title = opp.Title ?? "No Title",
                                    Location = opp.Location ?? string.Empty,
                                    IsRemote = opp.IsRemote ?? false,
                                    Type = opp.Type ?? "Other",
                                    Status = opp.Status ?? "Draft",
                                    ApplicationDeadline = opp.ApplicationDeadline ?? DateTime.UtcNow.AddDays(30),
                                    CreatedAt = opp.CreatedAt ?? DateTime.UtcNow,
                                    CategoryName = opp.CategoryName ?? "Uncategorized",
                                    TalentArea = opp.TalentArea ?? string.Empty,
                                    PostedBy = opp.PostedBy ?? "System",
                                    ApplicationCount = opp.ApplicationCount ?? 0
                                });
                            }
                        }
                    }
                    // Map active contests
                    if (dashboardData.ActiveContests != null)
                    {
                        viewModel.ActiveContests = new List<ContestViewModel>();
                        foreach (var contest in dashboardData.ActiveContests)
                        {
                            if (contest != null)
                            {
                                viewModel.ActiveContests.Add(new ContestViewModel
                                {
                                    ContestID = contest.ContestID ?? 0,
                                    Title = contest.Title ?? "Untitled Contest",
                                    Description = contest.Description ?? string.Empty,
                                    StartDate = contest.StartDate ?? DateTime.UtcNow,
                                    EndDate = contest.EndDate ?? DateTime.UtcNow.AddDays(30),
                                    IsClosed = contest.IsClosed ?? false
                                });
                            }
                        }
                    }
            // Map stats
            viewModel.Stats = new StatsViewModel
            {
                // User stats
                TotalUsers = dashboardData.Stats?.TotalUsers ?? 0,
                NewUsersThisMonth = dashboardData.Stats?.NewUsersThisMonth ?? 0,
                ActiveUsersToday = dashboardData.Stats?.ActiveUsersToday ?? 0,
                UserGrowthPercentage = dashboardData.Stats?.UserGrowthPercentage ?? 0,
                
                // Opportunity stats
                TotalOpportunities = dashboardData.Stats?.TotalOpportunities ?? 0,
                OpenOpportunities = dashboardData.Stats?.OpenOpportunities ?? 0,
                ClosedOpportunities = dashboardData.Stats?.ClosedOpportunities ?? 0,
                ApplicationsThisMonth = dashboardData.Stats?.ApplicationsThisMonth ?? 0,
                ApplicationGrowthPercentage = dashboardData.Stats?.ApplicationGrowthPercentage ?? 0,
                
                // Contest stats
                TotalContests = dashboardData.Stats?.TotalContests ?? 0,
                ActiveContests = dashboardData.Stats?.ActiveContests ?? 0,
                TotalContestEntries = dashboardData.Stats?.TotalContestEntries ?? 0,
                ContestEntriesThisMonth = dashboardData.Stats?.ContestEntriesThisMonth ?? 0,
                
                // Content stats
                TotalPosts = dashboardData.Stats?.TotalPosts ?? 0,
                TotalCategories = dashboardData.Stats?.TotalCategories ?? 0,
                TotalComments = dashboardData.Stats?.TotalComments ?? 0,
                TotalLikes = dashboardData.Stats?.TotalLikes ?? 0
            };

            // Map recent activities
            if (dashboardData.RecentActivities != null)
            {
                viewModel.RecentActivities = new List<RecentActivityViewModel>();
                foreach (var activity in dashboardData.RecentActivities)
                {
                    if (activity != null)
                    {
                        viewModel.RecentActivities.Add(new RecentActivityViewModel
                        {
                            Id = activity.Id,
                            Type = activity.Type ?? string.Empty,
                            Title = activity.Title ?? string.Empty,
                            Description = activity.Description ?? string.Empty,
                            Timestamp = activity.Timestamp,
                            UserName = activity.UserName ?? "Unknown User",
                            UserAvatar = activity.UserAvatar
                        });
                    }
                }
            }


            // Map application statuses
            if (dashboardData.ApplicationStatuses != null)
            {
                viewModel.ApplicationStatuses = new List<ApplicationStatusViewModel>();
                foreach (var status in dashboardData.ApplicationStatuses)
                {
                    if (status != null)
                    {
                        viewModel.ApplicationStatuses.Add(new ApplicationStatusViewModel
                        {
                            Status = status.Status ?? "Unknown",
                            Count = status.Count,
                            Percentage = status.Percentage
                        });
                    }
                }
            }


            // Map opportunity trends
            if (dashboardData.OpportunityTrends != null)
            {
                viewModel.OpportunityTrends = new List<OpportunityTrendViewModel>();
                foreach (var trend in dashboardData.OpportunityTrends)
                {
                    if (trend != null)
                    {
                        viewModel.OpportunityTrends.Add(new OpportunityTrendViewModel
                        {
                            Period = trend.Period ?? string.Empty,
                            Opportunities = trend.Opportunities,
                            Applications = trend.Applications
                        });
                    }
                }
            }


            // Map recent opportunities
            if (dashboardData.RecentOpportunities != null)
            {
                viewModel.RecentOpportunities = new List<RecentOpportunityViewModel>();
                foreach (var opp in dashboardData.RecentOpportunities)
                {
                    if (opp != null)
                    {
                        viewModel.RecentOpportunities.Add(new RecentOpportunityViewModel
                        {
                            Id = opp.Id ?? 0,
                            Title = opp.Title ?? "No Title",
                            Location = opp.Location ?? string.Empty,
                            IsRemote = opp.IsRemote ?? false,
                            Type = opp.Type ?? "Other",
                            Status = opp.Status ?? "Draft",
                            ApplicationDeadline = opp.ApplicationDeadline ?? DateTime.UtcNow.AddDays(30),
                            CreatedAt = opp.CreatedAt ?? DateTime.UtcNow,
                            CategoryName = opp.CategoryName ?? "Uncategorized",
                            TalentArea = opp.TalentArea ?? string.Empty,
                            PostedBy = opp.PostedBy ?? "System",
                            ApplicationCount = opp.ApplicationCount ?? 0
                        });
                    }
                }
            }


            // Map active contests
            if (dashboardData.ActiveContests != null)
            {
                viewModel.ActiveContests = new List<ContestViewModel>();
                foreach (var contest in dashboardData.ActiveContests)
                {
                    if (contest != null)
                    {
                        viewModel.ActiveContests.Add(new ContestViewModel
                        {
                            ContestID = contest.ContestID ?? 0,
                            Title = contest.Title ?? "Untitled Contest",
                            Description = contest.Description ?? string.Empty,
                            StartDate = contest.StartDate ?? DateTime.UtcNow,
                            EndDate = contest.EndDate ?? DateTime.UtcNow.AddDays(30),
                            IsClosed = contest.IsClosed ?? false
                        });
                    }
                }
            }
        }
        catch (JsonException jsonEx)
        {
            _logger.LogError(jsonEx, "Error deserializing dashboard data");
            return View(viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred in Dashboard/Index");
            return View(viewModel);
        }

        return View(viewModel);
    }
}