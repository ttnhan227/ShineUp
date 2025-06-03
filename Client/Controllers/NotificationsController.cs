using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Client.Models;
using System.Text.Json;
using System.Text;
using Microsoft.Extensions.Logging;

namespace Client.Controllers;

[Authorize]
[Route("notifications")]
public class NotificationsController : Controller
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<NotificationsController> _logger;
    private readonly IConfiguration _configuration;

    public NotificationsController(
        IHttpClientFactory httpClientFactory,
        ILogger<NotificationsController> logger,
        IConfiguration configuration)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _configuration = configuration;
    }

    private HttpClient CreateAuthenticatedClient()
    {
        var client = _httpClientFactory.CreateClient("API");
        var token = HttpContext.Request.Cookies["auth_token"];
        if (!string.IsNullOrEmpty(token))
        {
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        }
        return client;
    }

    [HttpGet]
    public async Task<IActionResult> Index([FromQuery] bool unreadOnly = false)
    {
        try
        {
            var client = CreateAuthenticatedClient();
            var response = await client.GetAsync($"api/notifications?unreadOnly={unreadOnly}");
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError($"Failed to fetch notifications. Status: {response.StatusCode}, Response: {errorContent}");
                return View("Error");
            }

            var content = await response.Content.ReadAsStringAsync();
            _logger.LogInformation($"Received notifications: {content}");
            
            var notifications = JsonSerializer.Deserialize<List<NotificationViewModel>>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? new List<NotificationViewModel>();

            _logger.LogInformation($"Deserialized {notifications.Count} notifications");
            
            ViewBag.UnreadCount = notifications.Count(n => n.Status == NotificationStatus.Unread);
            ViewBag.TotalCount = notifications.Count;
            ViewBag.ShowUnreadOnly = unreadOnly;

            return View(notifications);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in Notifications/Index");
            return View("Error");
        }
    }

    // GET: /notifications/5
    [HttpGet("{id}")]
    public async Task<IActionResult> Details(int id)
    {
        try
        {
            var client = CreateAuthenticatedClient();
            var response = await client.GetAsync($"api/notifications/{id}");
            
            if (!response.IsSuccessStatusCode)
            {
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return NotFound();
                }
                return View("Error");
            }

            var content = await response.Content.ReadAsStringAsync();
            var notification = JsonSerializer.Deserialize<NotificationViewModel>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            // Mark as read when viewing details
            if (notification.Status == NotificationStatus.Unread)
            {
                await MarkAsRead(id);
            }

            return View(notification);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error fetching notification with ID {id}");
            return View("Error");
        }
    }

    // POST: /notifications/5/read
    [HttpPost("{id}/read")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAsRead(int id)
    {
        try
        {
            var client = CreateAuthenticatedClient();
            var response = await client.PutAsync($"api/notifications/{id}/read", null);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError($"Failed to mark notification {id} as read. Status: {response.StatusCode}");
                return Json(new { success = false });
            }

            return Json(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error marking notification {id} as read");
            return Json(new { success = false });
        }
    }

    // POST: /notifications/read-all
    [HttpPost("read-all")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAllAsRead()
    {
        try
        {
            var client = CreateAuthenticatedClient();
            var response = await client.PutAsync("api/notifications/read-all", null);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError($"Failed to mark all notifications as read. Status: {response.StatusCode}");
                TempData["ErrorMessage"] = "Failed to mark all notifications as read.";
                return RedirectToAction(nameof(Index));
            }

            TempData["SuccessMessage"] = "All notifications marked as read.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking all notifications as read");
            TempData["ErrorMessage"] = "An error occurred while marking notifications as read.";
            return RedirectToAction(nameof(Index));
        }
    }

    // DELETE: /notifications/5
    [HttpDelete("{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var client = CreateAuthenticatedClient();
            var response = await client.DeleteAsync($"api/notifications/{id}");
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError($"Failed to delete notification {id}. Status: {response.StatusCode}");
                return Json(new { success = false });
            }

            return Json(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error deleting notification {id}");
            return Json(new { success = false });
        }
    }

    [HttpGet("unread-count")]
public async Task<IActionResult> GetUnreadCount()
{
    try
    {
        var client = CreateAuthenticatedClient();
        var response = await client.GetAsync("api/notifications?unreadOnly=true");
        
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError($"Failed to get unread notifications. Status: {response.StatusCode}");
            return Json(new { count = 0 });
        }

        var content = await response.Content.ReadAsStringAsync();
        _logger.LogInformation($"Unread notifications response: {content}"); // Add this line
        
        var unreadNotifications = JsonSerializer.Deserialize<List<NotificationViewModel>>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
        
        return Json(new { count = unreadNotifications?.Count ?? 0 });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error getting unread notification count");
        return Json(new { count = 0 });
    }
}

    // GET: /notifications/preferences
    [HttpGet("preferences")]
    public IActionResult Preferences()
    {
        // In a real app, you would fetch these from the user's profile
        var model = new NotificationPreferencesViewModel();
        return View(model);
    }

    // POST: /notifications/preferences
    [HttpPost("preferences")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SavePreferences(NotificationPreferencesViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View("Preferences", model);
        }

        try
        {
            // In a real app, you would save these preferences to the user's profile
            TempData["SuccessMessage"] = "Notification preferences updated successfully!";
            return RedirectToAction(nameof(Preferences));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving notification preferences");
            ModelState.AddModelError(string.Empty, "An error occurred while saving your preferences.");
            return View("Preferences", model);
        }
    }
}
