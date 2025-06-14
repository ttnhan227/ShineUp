using Client.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Client.Controllers;

[Authorize]
[Route("notifications")]
public class NotificationsController : Controller
{
    private readonly IHttpClientFactory _clientFactory;
    private readonly IConfiguration _configuration;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly ILogger<NotificationsController> _logger;

    public NotificationsController(
        IHttpClientFactory clientFactory,
        IConfiguration configuration,
        ILogger<NotificationsController> logger)
    {
        _clientFactory = clientFactory;
        _configuration = configuration;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter() }
        };
    }

    private async Task<HttpClient> GetAuthenticatedClient()
    {
        // Check if user is authenticated
        if (!User.Identity.IsAuthenticated)
        {
            _logger.LogWarning("User is not authenticated");
            return null;
        }

        // Get the JWT token from the claims
        var token = User.FindFirst("JWT")?.Value;
        if (string.IsNullOrEmpty(token))
        {
            _logger.LogWarning("JWT token not found in claims");
            return null;
        }

        var client = _clientFactory.CreateClient("API");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        _logger.LogInformation("JWT token added to request");

        return client;
    }

    // GET: Notifications
    [HttpGet]
    public async Task<IActionResult> Index(bool unreadOnly = false)
    {
        var client = await GetAuthenticatedClient();
        if (client == null)
        {
            return RedirectToAction("Login", "Auth");
        }

        var apiUrl = $"api/notifications?unreadOnly={unreadOnly}";

        try
        {
            var response = await client.GetAsync(apiUrl);
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var notifications = JsonSerializer.Deserialize<List<NotificationViewModel>>(content, _jsonOptions);
                return View(notifications);
            }

            return HandleError(response.StatusCode);
        }
        catch (HttpRequestException)
        {
            return View("Error", new ErrorViewModel { RequestId = HttpContext.TraceIdentifier });
        }
    }

    // GET: Notifications/Details/5
    [HttpGet("details/{id}")]
    public async Task<IActionResult> Details(int id)
    {
        var client = await GetAuthenticatedClient();
        if (client == null)
        {
            return RedirectToAction("Login", "Auth");
        }

        var apiUrl = $"api/notifications/{id}";

        try
        {
            var response = await client.GetAsync(apiUrl);
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var notification = JsonSerializer.Deserialize<NotificationViewModel>(content, _jsonOptions);
                return View(notification);
            }

            return HandleError(response.StatusCode);
        }
        catch (HttpRequestException)
        {
            return View("Error", new ErrorViewModel { RequestId = HttpContext.TraceIdentifier });
        }
    }

    // POST: Notifications/MarkAsRead/5
    [HttpPost("mark-as-read/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAsRead(int id)
    {
        var client = await GetAuthenticatedClient();
        if (client == null)
        {
            return RedirectToAction("Login", "Auth");
        }

        var apiUrl = $"api/notifications/{id}/read";

        try
        {
            var response = await client.PutAsync(apiUrl, null);
            if (response.IsSuccessStatusCode)
            {
                return RedirectToAction(nameof(Index));
            }

            return HandleError(response.StatusCode);
        }
        catch (HttpRequestException)
        {
            return View("Error", new ErrorViewModel { RequestId = HttpContext.TraceIdentifier });
        }
    }

    // POST: Notifications/MarkAllAsRead
    [HttpPost("mark-all-read")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAllAsRead()
    {
        var client = await GetAuthenticatedClient();
        if (client == null)
        {
            return RedirectToAction("Login", "Auth");
        }

        var apiUrl = "api/notifications/read-all";

        try
        {
            var response = await client.PutAsync(apiUrl, null);
            if (response.IsSuccessStatusCode)
            {
                return RedirectToAction(nameof(Index));
            }

            return HandleError(response.StatusCode);
        }
        catch (HttpRequestException)
        {
            return View("Error", new ErrorViewModel { RequestId = HttpContext.TraceIdentifier });
        }
    }

    // GET: Notifications/UnreadCount
    // GET: Notifications/Recent
    [HttpGet("recent")]
    public async Task<IActionResult> Recent()
    {
        _logger.LogInformation("Getting recent notifications");
        var client = await GetAuthenticatedClient();
        if (client == null)
        {
            _logger.LogWarning("Unauthorized access to recent notifications");
            return Unauthorized();
        }

        try
        {
            var response = await client.GetAsync("api/Notifications/recent");
            var content = await response.Content.ReadAsStringAsync();
            _logger.LogInformation($"Response status: {response.StatusCode}");
            _logger.LogInformation($"Response content: {content}");

            if (response.IsSuccessStatusCode)
            {
                try
                {
                    var notifications = JsonSerializer.Deserialize<List<NotificationViewModel>>(content, _jsonOptions);
                    _logger.LogInformation($"Successfully deserialized {notifications?.Count ?? 0} notifications");
                    return Ok(notifications);
                }
                catch (JsonException ex)
                {
                    _logger.LogError(ex, "Error deserializing notifications");
                    return StatusCode(500, new { error = "Error processing notifications" });
                }
            }

            _logger.LogError($"Error from API: {response.StatusCode} - {content}");
            return StatusCode((int)response.StatusCode, content);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Error getting recent notifications");
            return StatusCode(500, new { error = "Error getting recent notifications" });
        }
    }

    // GET: Notifications/UnreadCount
    [HttpGet("unread-count")]
    public async Task<IActionResult> GetUnreadCount()
    {
        var client = await GetAuthenticatedClient();
        if (client == null)
        {
            return Json(new { count = 0 });
        }

        try
        {
            var response = await client.GetAsync("api/Notifications/unread-count");
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var count = JsonSerializer.Deserialize<int>(content, _jsonOptions);
                return Json(new { count });
            }

            return Json(new { count = 0 });
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Error getting unread count");
            return Json(new { count = 0 });
        }
    }

    // GET: Notifications/GetRecentNotifications
    [HttpGet("recent-notifications")]
    public async Task<IActionResult> GetRecentNotifications()
    {
        var client = await GetAuthenticatedClient();
        if (client == null)
        {
            return Json(Array.Empty<NotificationViewModel>());
        }

        try
        {
            var response = await client.GetAsync("api/Notifications/recent");
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var notifications = JsonSerializer.Deserialize<List<NotificationViewModel>>(content, _jsonOptions);
                return Json(notifications ?? new List<NotificationViewModel>());
            }

            return Json(new List<NotificationViewModel>());
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Error getting recent notifications");
            return Json(new List<NotificationViewModel>());
        }
    }

    // POST: Notifications/Delete/5
    [HttpPost("delete/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var client = await GetAuthenticatedClient();
        if (client == null)
        {
            return Json(new { success = false, message = "Unauthorized" });
        }

        var apiUrl = $"api/notifications/{id}";

        try
        {
            var response = await client.DeleteAsync(apiUrl);
            if (response.IsSuccessStatusCode)
            {
                return Json(new { success = true });
            }

            _logger.LogError($"Failed to delete notification {id}. Status: {response.StatusCode}");
            return Json(new { success = false, message = "Failed to delete notification" });
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, $"Error deleting notification {id}");
            return Json(new { success = false, message = "Error deleting notification" });
        }
    }

    private IActionResult HandleError(HttpStatusCode statusCode)
    {
        return statusCode switch
        {
            HttpStatusCode.Unauthorized => RedirectToAction("Login", "Auth"),
            HttpStatusCode.NotFound => View("NotFound"),
            _ => View("Error", new ErrorViewModel { RequestId = HttpContext.TraceIdentifier })
        };
    }
}