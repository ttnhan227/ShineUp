using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Security.Claims;
using Client.Models;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using System.Text;
using Microsoft.AspNetCore.Authorization;

namespace Client.Controllers
{
    public class CommunityController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<CommunityController> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CommunityController(
            IHttpClientFactory httpClientFactory,
            ILogger<CommunityController> logger,
            IHttpContextAccessor httpContextAccessor)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
        }

        private HttpClient CreateAuthenticatedClient()
        {
            var client = _httpClientFactory.CreateClient("BackendAPI");
            var token = _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.Authentication);
            
            if (!string.IsNullOrEmpty(token))
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
            
            // Ensure we're sending JSON
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            
            return client;
        }

        private string? GetToken() => User.FindFirstValue(ClaimTypes.Authentication);

        private int GetUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);
            return claim != null && int.TryParse(claim.Value, out int id) ? id : 0;
        }

        [HttpGet]
        public IActionResult Create()
        {
            if (!User.Identity.IsAuthenticated)
                return RedirectToAction("Login", "Account");

            return View(new CommunityViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CommunityViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            try
            {
                using var client = CreateAuthenticatedClient();
                var response = await client.PostAsJsonAsync("/api/community", model);
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var createdCommunity = JsonSerializer.Deserialize<CommunityViewModel>(content, options);
                    
                    if (createdCommunity != null)
                    {
                        TempData["Success"] = "Tạo cộng đồng thành công!";
                        return RedirectToAction(nameof(Details), new { communityId = createdCommunity.CommunityID });
                    }
                }
                
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Failed to create community. Status: {StatusCode}, Response: {Response}", 
                    response.StatusCode, errorContent);
                
                ModelState.AddModelError(string.Empty, "Không thể tạo cộng đồng. Vui lòng thử lại sau.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating community");
                ModelState.AddModelError(string.Empty, "Đã xảy ra lỗi khi tạo cộng đồng.");
            }
            
            return View(model);
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                using var client = CreateAuthenticatedClient();
                var response = await client.GetAsync("/api/community");
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var communities = JsonSerializer.Deserialize<List<CommunityViewModel>>(content, options);
                    
                    if (communities != null)
                    {
                        return View(communities);
                    }
                    
                    _logger.LogWarning("Failed to deserialize communities response");
                }
                else
                {
                    _logger.LogWarning("Failed to get communities. Status: {StatusCode}", response.StatusCode);
                }
                
                TempData["Error"] = "Không thể lấy danh sách cộng đồng.";
                return View(new List<CommunityViewModel>());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Index()");
                TempData["Error"] = "Đã xảy ra lỗi hệ thống.";
                return View(new List<CommunityViewModel>());
            }
        }

        public async Task<IActionResult> Details(int communityId)
        {
            if (communityId <= 0) return BadRequest("CommunityId không hợp lệ");

            if (!User.Identity.IsAuthenticated)
                return RedirectToAction("Login", "Account");

            try
            {
                using var client = CreateAuthenticatedClient();
                var response = await client.GetAsync($"/api/community/{communityId}");
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var responseData = JsonSerializer.Deserialize<CommunityDetailsResponse>(content, options);

                    if (responseData != null)
                    {
                        var userId = GetUserId();
                        var userRole = responseData.UserRole;

                        var viewModel = new CommunityDetailsViewModel
                        {
                            Community = new CommunityViewModel
                            {
                                CommunityID = responseData.CommunityID,
                                Name = responseData.Name,
                                Description = responseData.Description,
                                CreatedAt = responseData.CreatedAt,
                                UpdatedAt = responseData.UpdatedAt
                            },
                            Members = responseData.Members.Select(m => new CommunityMemberViewModel
                            {
                                UserID = m.UserID,
                                User = new UserViewModel
                                {
                                    UserID = m.UserID,
                                    Username = m.Username,
                                    FullName = m.FullName,
                                    Email = m.Email
                                },
                                CommunityID = responseData.CommunityID,
                                Role = m.Role == "Admin" ? CommunityRole.Admin : CommunityRole.Member,
                                JoinedAt = m.JoinedAt,
                                LastActiveAt = m.LastActiveAt
                            }).ToList(),
                            UserRole = userRole
                        };

                        ViewBag.UserRole = userRole;
                        ViewBag.CurrentUserId = userId;
                        return View(viewModel);
                    }
                    
                    _logger.LogWarning("Failed to deserialize community details");
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    return RedirectToAction("Login", "Account");
                }
                else
                {
                    _logger.LogWarning("Failed to get community details. Status: {StatusCode}", response.StatusCode);
                }

                TempData["Error"] = "Không thể lấy thông tin chi tiết cộng đồng.";
                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting community details for communityId={CommunityId}", communityId);
                TempData["Error"] = "Đã xảy ra lỗi hệ thống.";
                return View();
            }
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Join(int communityId)
        {
            if (communityId <= 0) return BadRequest("CommunityId không hợp lệ");

            try
            {
                using var client = CreateAuthenticatedClient();
                var response = await client.PostAsync($"/api/community/{communityId}/join", null);
                
                if (response.IsSuccessStatusCode)
                {
                    TempData["Success"] = "Đã tham gia cộng đồng thành công.";
                    return RedirectToAction(nameof(Details), new { communityId });
                }
                
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Failed to join community. Status: {StatusCode}, Response: {Response}", 
                    response.StatusCode, errorContent);
                
                TempData["Error"] = response.StatusCode == System.Net.HttpStatusCode.BadRequest 
                    ? errorContent 
                    : "Không thể tham gia cộng đồng. Vui lòng thử lại sau.";
                
                return RedirectToAction(nameof(Details), new { communityId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error joining communityId={CommunityId}", communityId);
                TempData["Error"] = "Đã xảy ra lỗi hệ thống.";
                return RedirectToAction(nameof(Details), new { communityId });
            }
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Leave(int communityId)
        {
            if (communityId <= 0) return BadRequest("CommunityId không hợp lệ");

            try
            {
                using var client = CreateAuthenticatedClient();
                var response = await client.PostAsync($"/api/community/{communityId}/leave", null);
                
                if (response.IsSuccessStatusCode)
                {
                    TempData["Success"] = "Đã rời khỏi cộng đồng thành công.";
                    return RedirectToAction(nameof(Index));
                }
                
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Failed to leave community. Status: {StatusCode}, Response: {Response}", 
                    response.StatusCode, errorContent);
                
                TempData["Error"] = response.StatusCode == System.Net.HttpStatusCode.BadRequest 
                    ? errorContent 
                    : "Không thể rời khỏi cộng đồng. Vui lòng thử lại sau.";
                
                return RedirectToAction(nameof(Details), new { communityId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error leaving communityId={CommunityId}", communityId);
                TempData["Error"] = "Đã xảy ra lỗi hệ thống.";
                return RedirectToAction(nameof(Details), new { communityId });
            }
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> RemoveMember(int communityId, int userId)
        {
            if (communityId <= 0 || userId <= 0) 
                return BadRequest("Thông tin không hợp lệ");

            try
            {
                using var client = CreateAuthenticatedClient();
                var response = await client.DeleteAsync($"/api/community/{communityId}/members/{userId}");
                
                if (response.IsSuccessStatusCode)
                {
                    TempData["Success"] = "Đã xoá thành viên khỏi cộng đồng thành công.";
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("Failed to remove member. Status: {StatusCode}, Response: {Response}", 
                        response.StatusCode, errorContent);
                    
                    TempData["Error"] = response.StatusCode == System.Net.HttpStatusCode.BadRequest 
                        ? errorContent 
                        : "Không thể xoá thành viên. Vui lòng thử lại sau.";
                }

                return RedirectToAction(nameof(Details), new { communityId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing userId={UserId} from communityId={CommunityId}", userId, communityId);
                TempData["Error"] = "Đã xảy ra lỗi hệ thống.";
                return RedirectToAction(nameof(Details), new { communityId });
            }
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> TransferAdmin(int communityId, int newAdminId)
        {
            if (communityId <= 0 || newAdminId <= 0) 
                return BadRequest("Thông tin không hợp lệ");

            try
            {
                using var client = CreateAuthenticatedClient();
                var response = await client.PostAsJsonAsync($"/api/community/{communityId}/transfer-admin?newAdminId={newAdminId}", new { });
                
                if (response.IsSuccessStatusCode)
                {
                    TempData["Success"] = "Đã chuyển quyền quản trị thành công.";
                    return RedirectToAction(nameof(Details), new { communityId });
                }
                
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Failed to transfer admin. Status: {StatusCode}, Response: {Response}", 
                    response.StatusCode, errorContent);
                
                TempData["Error"] = response.StatusCode == System.Net.HttpStatusCode.BadRequest 
                    ? errorContent 
                    : "Không thể chuyển quyền quản trị. Vui lòng thử lại sau.";
                
                return RedirectToAction(nameof(Details), new { communityId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error transferring admin for communityId={CommunityId} to userId={NewAdminId}", communityId, newAdminId);
                TempData["Error"] = "Đã xảy ra lỗi hệ thống.";
                return RedirectToAction(nameof(Details), new { communityId });
            }
        }

        public class CommunityDetailsResponse
        {
            public int CommunityID { get; set; }
            public string Name { get; set; } = string.Empty;
            public string? Description { get; set; }
            public DateTime CreatedAt { get; set; }
            public DateTime? UpdatedAt { get; set; }
            public List<CommunityMemberResponse> Members { get; set; } = new();
            public string UserRole { get; set; } = "None";
        }

        public class CommunityMemberResponse
        {
            public int UserID { get; set; }
            public string Username { get; set; } = string.Empty;
            public string FullName { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public string Role { get; set; } = "Member";
            public DateTime JoinedAt { get; set; }
            public DateTime? LastActiveAt { get; set; }
        }
        
        public class ApiResponse<T>
        {
            public bool Success { get; set; }
            public string? Message { get; set; }
            public T? Data { get; set; }
        }
    }
}
