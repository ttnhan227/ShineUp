using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using Client.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using System.Text.Json.Serialization;

namespace Client.Controllers
{
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

        private IActionResult HandleError(System.Net.HttpStatusCode statusCode)
        {
            return statusCode switch
            {
                System.Net.HttpStatusCode.Unauthorized => RedirectToAction("Login", "Auth"),
                System.Net.HttpStatusCode.NotFound => View("NotFound"),
                _ => View("Error", new ErrorViewModel { RequestId = HttpContext.TraceIdentifier })
            };
        }
    }
}