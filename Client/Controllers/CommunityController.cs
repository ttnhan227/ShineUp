using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using Client.DTOs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Client.Controllers
{
    public class CommunityController : Controller
    {
        private readonly IHttpClientFactory _clientFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<CommunityController> _logger;
        private readonly string _apiBaseUrl;

        public CommunityController(IHttpClientFactory clientFactory, IConfiguration configuration, ILogger<CommunityController> logger)
        {
            _clientFactory = clientFactory;
            _configuration = configuration;
            _logger = logger;
            _apiBaseUrl = _configuration["ApiBaseUrl"] ?? throw new ArgumentNullException("ApiBaseUrl not configured");
        }

        private string? GetToken() => User.FindFirstValue(ClaimTypes.Authentication);

        private int GetUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);
            return claim != null && int.TryParse(claim.Value, out int id) ? id : 0;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                var client = _clientFactory.CreateClient();
                var token = GetToken();

                if (token != null)
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var response = await client.GetAsync($"{_apiBaseUrl}/community");
                if (response.IsSuccessStatusCode)
                {
                    var communities = await response.Content.ReadFromJsonAsync<List<CommunityDTO>>();
                    return View(communities ?? new List<CommunityDTO>());
                }

                _logger.LogWarning("Không thể lấy danh sách cộng đồng. StatusCode: {StatusCode}", response.StatusCode);
                TempData["Error"] = "Không thể lấy danh sách cộng đồng.";
                return View(new List<CommunityDTO>());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi trong Index()");
                TempData["Error"] = "Đã xảy ra lỗi hệ thống.";
                return View(new List<CommunityDTO>());
            }
        }

        public async Task<IActionResult> Details(int communityId)
        {
            if (communityId <= 0) return BadRequest("CommunityId không hợp lệ");

            var token = GetToken();
            if (token == null) return RedirectToAction("Login", "Account");

            try
            {
                var client = _clientFactory.CreateClient();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var response = await client.GetAsync($"{_apiBaseUrl}/community/{communityId}/details");
                if (response.IsSuccessStatusCode)
                {
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var stream = await response.Content.ReadAsStreamAsync();
                    var detail = await JsonSerializer.DeserializeAsync<CommunityDetailsResponse>(stream, options);

                    if (detail == null) return NotFound();

                    ViewBag.UserRole = detail.UserRole;
                    ViewBag.CurrentUserId = GetUserId();

                    return View(detail);
                }

                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    return RedirectToAction("Login", "Account");

                _logger.LogWarning("Không thể lấy thông tin chi tiết communityId={CommunityId}", communityId);
                TempData["Error"] = "Lỗi khi lấy chi tiết cộng đồng.";
                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy chi tiết communityId={CommunityId}", communityId);
                TempData["Error"] = "Đã xảy ra lỗi hệ thống.";
                return View();
            }
        }

        [HttpPost]
        public async Task<IActionResult> Join(int communityId)
        {
            var token = GetToken();
            if (token == null) return RedirectToAction("Login", "Account");

            try
            {
                var client = _clientFactory.CreateClient();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var response = await client.PostAsync($"{_apiBaseUrl}/community/{communityId}/join", null);
                if (response.IsSuccessStatusCode)
                {
                    TempData["Success"] = "Đã tham gia cộng đồng.";
                    return RedirectToAction(nameof(Details), new { communityId });
                }

                TempData["Error"] = "Không thể tham gia cộng đồng.";
                return RedirectToAction(nameof(Details), new { communityId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tham gia communityId={CommunityId}", communityId);
                TempData["Error"] = "Lỗi hệ thống.";
                return RedirectToAction(nameof(Details), new { communityId });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Leave(int communityId)
        {
            var token = GetToken();
            if (token == null) return RedirectToAction("Login", "Account");

            try
            {
                var client = _clientFactory.CreateClient();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var response = await client.PostAsync($"{_apiBaseUrl}/community/{communityId}/leave", null);
                if (response.IsSuccessStatusCode)
                {
                    TempData["Success"] = "Đã rời khỏi cộng đồng.";
                    return RedirectToAction(nameof(Index));
                }

                TempData["Error"] = "Không thể rời khỏi cộng đồng.";
                return RedirectToAction(nameof(Details), new { communityId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi rời khỏi communityId={CommunityId}", communityId);
                TempData["Error"] = "Lỗi hệ thống.";
                return RedirectToAction(nameof(Details), new { communityId });
            }
        }

        [HttpPost]
        public async Task<IActionResult> RemoveMember(int communityId, int userId)
        {
            var token = GetToken();
            if (token == null) return RedirectToAction("Login", "Account");

            try
            {
                var client = _clientFactory.CreateClient();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var response = await client.DeleteAsync($"{_apiBaseUrl}/community/{communityId}/member/{userId}");
                if (response.IsSuccessStatusCode)
                    TempData["Success"] = "Đã xoá thành viên khỏi cộng đồng.";
                else
                    TempData["Error"] = "Không thể xoá thành viên.";

                return RedirectToAction(nameof(Details), new { communityId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xoá userId={UserId} khỏi communityId={CommunityId}", userId, communityId);
                TempData["Error"] = "Lỗi hệ thống.";
                return RedirectToAction(nameof(Details), new { communityId });
            }
        }

        [HttpPost]
        public async Task<IActionResult> TransferAdmin(int communityId, int newAdminId)
        {
            var token = GetToken();
            if (token == null) return RedirectToAction("Login", "Account");

            try
            {
                var client = _clientFactory.CreateClient();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var response = await client.PostAsync($"{_apiBaseUrl}/community/{communityId}/transfer-admin/{newAdminId}", null);
                if (response.IsSuccessStatusCode)
                    TempData["Success"] = "Đã chuyển quyền quản trị.";
                else
                    TempData["Error"] = "Không thể chuyển quyền quản trị.";

                return RedirectToAction(nameof(Details), new { communityId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi chuyển quyền quản trị communityId={CommunityId} cho userId={UserId}", communityId, newAdminId);
                TempData["Error"] = "Lỗi hệ thống.";
                return RedirectToAction(nameof(Details), new { communityId });
            }
        }

        public class CommunityDetailsResponse
        {
            public CommunityDTO Community { get; set; }
            public List<CommunityMemberDTO> Members { get; set; }
            public string UserRole { get; set; }
        }
    }
}
