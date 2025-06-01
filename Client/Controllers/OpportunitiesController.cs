using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Client.Models;
using Newtonsoft.Json;
using System.Text;
using Microsoft.AspNetCore.Http;
using System.Net.Http.Headers;

namespace Client.Controllers;

[Authorize]
public class OpportunitiesController : Controller
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<OpportunitiesController> _logger;
    private readonly IHttpClientFactory _httpClientFactory;

    public OpportunitiesController(IHttpClientFactory httpClientFactory, ILogger<OpportunitiesController> logger)
    {
        _httpClientFactory = httpClientFactory;
        _httpClient = _httpClientFactory.CreateClient("API");
        _logger = logger;
        
        // Set the base address from configuration
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json")
            .Build();
            
        var apiBaseUrl = configuration["ApiSettings:BaseUrl"];
        if (!string.IsNullOrEmpty(apiBaseUrl))
        {
            _httpClient.BaseAddress = new Uri(apiBaseUrl);
        }
    }

    public async Task<IActionResult> Index()
    {
        if (!User.Identity?.IsAuthenticated ?? true)
        {
            _logger.LogWarning("User is not authenticated");
            return RedirectToAction("Login", "Auth", new { returnUrl = Url.Action("Index", "Opportunities") });
        }

        try
        {
            // Try to get token from auth_token cookie first
            var token = HttpContext.Request.Cookies["auth_token"];
            
            // If not in cookie, try to get from claims (for backward compatibility)
            if (string.IsNullOrEmpty(token))
            {
                token = User.FindFirst("JWT")?.Value;
                _logger.LogInformation("Token found in claims");
            }
            
            if (string.IsNullOrEmpty(token))
            {
                _logger.LogWarning("JWT token not found in auth_token cookie or claims");
                return RedirectToAction("Login", "Auth", new { returnUrl = Url.Action("Index", "Opportunities") });
            }

            // Create a new HttpClient with the token
            var client = _httpClientFactory.CreateClient("API");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            // Get user ID from claims
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                              ?? User.FindFirst("UserID")?.Value 
                              ?? User.FindFirst("sub")?.Value;

            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                _logger.LogWarning("Invalid or missing user ID in claims");
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return RedirectToAction("Login", "Auth", new { returnUrl = Url.Action("Index", "Opportunities") });
            }

            // Fetch applications
            var applicationsResponse = await client.GetAsync($"api/opportunities/applications/user/{userId}");
            var applications = new List<OpportunityApplicationViewModel>();
            
            if (applicationsResponse.IsSuccessStatusCode)
            {
                applications = await applicationsResponse.Content.ReadFromJsonAsync<List<OpportunityApplicationViewModel>>() ?? new List<OpportunityApplicationViewModel>();
                
                // Format the dates for display
                foreach (var app in applications)
                {
                    if (DateTime.TryParse(app.AppliedAt, out var appliedAt))
                    {
                        app.AppliedAt = appliedAt.ToString("MMM dd, yyyy");
                    }
                }
            }
            else if (applicationsResponse.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                _logger.LogWarning("Unauthorized access to applications for user {UserId}", userId);
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return RedirectToAction("Login", "Auth");
            }
            else
            {
                var errorContent = await applicationsResponse.Content.ReadAsStringAsync();
                _logger.LogError("Error fetching applications: {StatusCode} - {Error}", 
                    applicationsResponse.StatusCode, errorContent);
            }

            // Fetch notifications
            var notificationsResponse = await client.GetAsync($"api/opportunities/notifications/user/{userId}");
            var notifications = new List<NotificationViewModel>();
            
            if (notificationsResponse.IsSuccessStatusCode)
            {
                notifications = await notificationsResponse.Content.ReadFromJsonAsync<List<NotificationViewModel>>() ?? new List<NotificationViewModel>();
                
                // Format the dates for display
                foreach (var notification in notifications)
                {
                    if (DateTime.TryParse(notification.CreatedAt, out var createdAt))
                    {
                        notification.CreatedAt = createdAt.ToString("MMM dd, yyyy");
                    }
                }
            }
            else if (notificationsResponse.StatusCode != System.Net.HttpStatusCode.NotFound)
            {
                var errorContent = await notificationsResponse.Content.ReadAsStringAsync();
                _logger.LogError("Error fetching notifications: {StatusCode} - {Error}", 
                    notificationsResponse.StatusCode, errorContent);
            }

            // Store notifications in ViewBag to be displayed in the layout
            ViewBag.Notifications = notifications;
            
            return View(applications);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in Opportunities/Index: {Message}", ex.Message);
            TempData["ErrorMessage"] = "An error occurred while loading your applications. Please try again later.";
            return View(new List<OpportunityApplicationViewModel>());
            return RedirectToAction("Index", "Home");
        }
    }


    // GET: Opportunities/Create
    public IActionResult Create()
    {
        if (!User.Identity?.IsAuthenticated ?? true)
            return RedirectToAction("Login", "Auth", new { returnUrl = Url.Action("Create", "Opportunities") });

        return View();
    }

    // POST: Opportunities/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(OpportunityApplicationViewModel model)
    {
        if (!User.Identity?.IsAuthenticated ?? true)
            return RedirectToAction("Login", "Auth");

        if (!ModelState.IsValid)
            return View(model);

        try
        {
            // Get the JWT token from the authentication cookie
            var token = await HttpContext.GetTokenAsync(CookieAuthenticationDefaults.AuthenticationScheme, "access_token");
            if (string.IsNullOrEmpty(token))
            {
                _logger.LogWarning("JWT token not found in authentication cookie");
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return RedirectToAction("Login", "Auth");
            }

            // Create a new HttpClient with the token
            var client = _httpClientFactory.CreateClient("API");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            // Get user ID from claims
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                            ?? User.FindFirst("UserID")?.Value 
                            ?? User.FindFirst("sub")?.Value;

            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                _logger.LogWarning("Invalid or missing user ID in claims");
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return RedirectToAction("Login", "Auth");
            }

            // Get username from claims
            var username = User.FindFirst(ClaimTypes.Name)?.Value 
                         ?? User.FindFirst("name")?.Value 
                         ?? User.Identity?.Name 
                         ?? "Unknown User";

            var response = await client.PostAsJsonAsync("api/opportunities/applications", new
            {
                UserID = userId,
                Username = username,
                OpportunityTitle = model.OpportunityTitle,
                OpportunityDescription = model.OpportunityDescription,
                AppliedAt = DateTime.UtcNow
            });

            if (response.IsSuccessStatusCode)
            {
                TempData["SuccessMessage"] = "Application submitted successfully!";
                return RedirectToAction(nameof(Index));
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                _logger.LogWarning("Unauthorized access attempt to create application");
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return RedirectToAction("Login", "Auth");
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Error creating application: {StatusCode} - {Error}", 
                    response.StatusCode, errorContent);
                ModelState.AddModelError(string.Empty, "An error occurred while creating the application. Please try again.");
                return View(model);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in Opportunities/Create: {Message}", ex.Message);
            ModelState.AddModelError(string.Empty, "An error occurred while creating the application. Please try again.");
            return View(model);
        }
    }

    // GET: Opportunities/Details/5
    public async Task<IActionResult> Details(int id)
    {
        if (!User.Identity?.IsAuthenticated ?? true)
        {
            _logger.LogWarning("User is not authenticated");
            return RedirectToAction("Login", "Auth", new { returnUrl = Url.Action("Details", "Opportunities", new { id }) });
        }

        try
        {
            // Try to get token from auth_token cookie first
            var token = HttpContext.Request.Cookies["auth_token"];
            
            // If not in cookie, try to get from claims (for backward compatibility)
            if (string.IsNullOrEmpty(token))
            {
                token = User.FindFirst("JWT")?.Value;
                _logger.LogInformation("Token found in claims for Details action");
            }
            
            if (string.IsNullOrEmpty(token))
            {
                _logger.LogWarning("JWT token not found in auth_token cookie or claims for Details action");
                return RedirectToAction("Login", "Auth", new { returnUrl = Url.Action("Details", "Opportunities", new { id }) });
            }

            // Create a new HttpClient with the token
            var client = _httpClientFactory.CreateClient("API");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            // Get the application details
            var response = await client.GetAsync($"api/opportunities/applications/{id}");
            
            if (response.IsSuccessStatusCode)
            {
                var viewModel = await response.Content.ReadFromJsonAsync<OpportunityApplicationViewModel>();
                if (viewModel != null)
                {
                    // Format the date for display if needed
                    if (DateTime.TryParse(viewModel.AppliedAt, out var appliedAt))
                    {
                        viewModel.AppliedAt = appliedAt.ToString("MMM dd, yyyy");
                    }
                    return View(viewModel);
                }
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return NotFound();
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                _logger.LogWarning("Unauthorized access attempt to view application details {ApplicationId}", id);
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return RedirectToAction("Login", "Auth");
            }
            
            return NotFound();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in Opportunities/Details: {Message}", ex.Message);
            TempData["ErrorMessage"] = "An error occurred while loading the application details. Please try again later.";
            return RedirectToAction(nameof(Index));
        }
    }

    // GET: Opportunities/Edit/5
    public async Task<IActionResult> Edit(int id)
    {
        if (!User.Identity?.IsAuthenticated ?? true)
        {
            _logger.LogWarning("User is not authenticated for Edit action");
            return RedirectToAction("Login", "Auth", new { returnUrl = Url.Action("Edit", "Opportunities", new { id }) });
        }

        try
        {
            // Try to get token from auth_token cookie first
            var token = HttpContext.Request.Cookies["auth_token"];
            
            // If not in cookie, try to get from claims (for backward compatibility)
            if (string.IsNullOrEmpty(token))
            {
                token = User.FindFirst("JWT")?.Value;
                _logger.LogInformation("Token found in claims for Edit action");
            }
            
            if (string.IsNullOrEmpty(token))
            {
                _logger.LogWarning("JWT token not found in auth_token cookie or claims for Edit action");
                return RedirectToAction("Login", "Auth", new { returnUrl = Url.Action("Edit", "Opportunities", new { id }) });
            }

            // Create a new HttpClient with the token
            var client = _httpClientFactory.CreateClient("API");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await client.GetAsync($"api/opportunities/applications/{id}");
            
            if (response.IsSuccessStatusCode)
            {
                var viewModel = await response.Content.ReadFromJsonAsync<OpportunityApplicationViewModel>();
                if (viewModel != null)
                {
                    // Format the date for display if needed
                    if (DateTime.TryParse(viewModel.AppliedAt, out var appliedAt))
                    {
                        viewModel.AppliedAt = appliedAt.ToString("MMM dd, yyyy");
                    }
                    return View(viewModel);
                }
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                _logger.LogWarning("Unauthorized access attempt to edit application {ApplicationId}", id);
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return RedirectToAction("Login", "Auth");
            }
            
            return NotFound();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in Opportunities/Edit GET: {Message}", ex.Message);
            TempData["ErrorMessage"] = "An error occurred while loading the application for editing. Please try again later.";
            return RedirectToAction(nameof(Index));
        }
    }

    // POST: Opportunities/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, OpportunityApplicationViewModel model)
    {
        if (!User.Identity?.IsAuthenticated ?? true)
        {
            _logger.LogWarning("User is not authenticated for Edit POST action");
            return RedirectToAction("Login", "Auth");
        }

        if (!ModelState.IsValid)
            return View(model);

        try
        {
            // Try to get token from auth_token cookie first
            var token = HttpContext.Request.Cookies["auth_token"];
            
            // If not in cookie, try to get from claims (for backward compatibility)
            if (string.IsNullOrEmpty(token))
            {
                token = User.FindFirst("JWT")?.Value;
                _logger.LogInformation("Token found in claims for Edit POST action");
            }
            
            if (string.IsNullOrEmpty(token))
            {
                _logger.LogWarning("JWT token not found in auth_token cookie or claims for Edit POST action");
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return RedirectToAction("Login", "Auth");
            }

            // Create a new HttpClient with the token
            var client = _httpClientFactory.CreateClient("API");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await client.PutAsJsonAsync($"api/opportunities/applications/{id}", new
            {
                ApplicationID = id,
                UserID = model.UserID,
                Username = model.Username,
                OpportunityTitle = model.OpportunityTitle,
                OpportunityDescription = model.OpportunityDescription,
                AppliedAt = model.AppliedAt
            });

            if (response.IsSuccessStatusCode)
            {
                TempData["SuccessMessage"] = "Application updated successfully!";
                return RedirectToAction(nameof(Index));
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return NotFound();
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                _logger.LogWarning("Unauthorized access attempt to update application {ApplicationId}", id);
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return RedirectToAction("Login", "Auth");
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Error updating application {ApplicationId}: {StatusCode} - {Error}", 
                    id, response.StatusCode, errorContent);
                ModelState.AddModelError(string.Empty, "An error occurred while updating the application. Please try again.");
                return View(model);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in Opportunities/Edit POST: {Message}", ex.Message);
            ModelState.AddModelError(string.Empty, "An error occurred while updating the application. Please try again.");
            return View(model);
        }
    }

    // POST: Opportunities/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        if (!User.Identity?.IsAuthenticated ?? true)
        {
            _logger.LogWarning("User is not authenticated for Delete action");
            return RedirectToAction("Login", "Auth");
        }

        try
        {
            // Try to get token from auth_token cookie first
            var token = HttpContext.Request.Cookies["auth_token"];
            
            // If not in cookie, try to get from claims (for backward compatibility)
            if (string.IsNullOrEmpty(token))
            {
                token = User.FindFirst("JWT")?.Value;
                _logger.LogInformation("Token found in claims for Delete action");
            }
            
            if (string.IsNullOrEmpty(token))
            {
                _logger.LogWarning("JWT token not found in auth_token cookie or claims for Delete action");
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return RedirectToAction("Login", "Auth");
            }

            // Create a new HttpClient with the token
            var client = _httpClientFactory.CreateClient("API");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await client.DeleteAsync($"api/opportunities/applications/{id}");

            if (response.IsSuccessStatusCode)
            {
                TempData["SuccessMessage"] = "Application deleted successfully!";
                return RedirectToAction(nameof(Index));
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return NotFound();
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                _logger.LogWarning("Unauthorized access attempt to delete application {ApplicationId}", id);
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return RedirectToAction("Login", "Auth");
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Error deleting application {ApplicationId}: {StatusCode} - {Error}", 
                    id, response.StatusCode, errorContent);
                TempData["ErrorMessage"] = "An error occurred while deleting the application. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in Opportunities/Delete: {Message}", ex.Message);
            TempData["ErrorMessage"] = "An error occurred while deleting the application. Please try again.";
            return RedirectToAction(nameof(Index));
        }
    }
}

// Using ViewModels from Client.Models namespace instead of DTOs