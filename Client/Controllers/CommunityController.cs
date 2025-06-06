using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Security.Claims;
using Client.Models;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using System.IO;
using System.Net.Http;

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
            var token = _httpContextAccessor.HttpContext?.User.FindFirstValue("JWT");

            if (string.IsNullOrEmpty(token))
            {
                _logger.LogWarning("JWT token not found in user claims");
            }
            else
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                _logger.LogInformation("JWT token added to request headers");
            }

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
                return RedirectToAction("Login", "Auth");

            return View(new CommunityViewModel());
        }
        
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Edit([FromQuery] int communityId)
        {
            if (communityId <= 0)
                return BadRequest("CommunityId không hợp lệ");

            try
            {
                using var client = CreateAuthenticatedClient();
                _logger.LogInformation("Fetching community details for editing. CommunityId: {CommunityId}", communityId);
                var response = await client.GetAsync($"api/community/{communityId}");
                var responseContent = await response.Content.ReadAsStringAsync();
                
                _logger.LogInformation("API Response Status: {StatusCode}", response.StatusCode);
                _logger.LogDebug("API Response Content: {ResponseContent}", responseContent);

                if (response.IsSuccessStatusCode)
                {
                    try
                    {
                        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                        var community = JsonSerializer.Deserialize<CommunityViewModel>(responseContent, options);

                        if (community != null)
                        {
                            _logger.LogInformation("Successfully deserialized community. ID: {CommunityId}, Name: {Name}", 
                                community.CommunityID, community.Name);
                                
                            var editModel = new EditCommunityViewModel
                            {
                                CommunityID = community.CommunityID,
                                Name = community.Name,
                                Description = community.Description,
                                CurrentCoverImageUrl = community.CoverImageUrl,
                                PrivacyID = community.PrivacyID
                            };
                            
                            return View(editModel);
                        }
                        else
                        {
                            _logger.LogWarning("Failed to deserialize community. Response was null or invalid.");
                        }
                    }
                    catch (JsonException jsonEx)
                    {
                        _logger.LogError(jsonEx, "JSON deserialization error. Response: {ResponseContent}", responseContent);
                        throw; // This will be caught by the outer catch block
                    }
                }
                
                TempData["Error"] = "Community not found or you don't have permission to edit.";
                return RedirectToAction("Details", new { communityId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading community for editing. CommunityId: {CommunityId}", communityId);
                TempData["Error"] = "An error occurred while loading community information.";
                return RedirectToAction("Details", new { communityId });
            }
        }
        
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit([FromForm] int communityId, EditCommunityViewModel model)
        {
            if (communityId <= 0)
            {
                ModelState.AddModelError("", "CommunityId không hợp lệ");
                return View(model);
            }

            // Ensure the model's CommunityID matches the one from the form
            if (model.CommunityID != communityId)
            {
                _logger.LogWarning("Community ID mismatch. Model: {ModelId}, Form: {FormId}", model.CommunityID, communityId);
                ModelState.AddModelError("", "Invalid data");
                return View(model);
            }

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Model state is invalid. CommunityId: {CommunityId}, Errors: {Errors}", communityId, 
                    string.Join("; ", ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)));
                return View(model);
            }

            try
            {
                using var client = CreateAuthenticatedClient();
                
                // Create multipart form data
                using var formData = new MultipartFormDataContent();
                
                // Add model properties to form data
                formData.Add(new StringContent(model.CommunityID.ToString()), "CommunityID");
                formData.Add(new StringContent(model.Name), "Name");
                formData.Add(new StringContent(model.Description ?? string.Empty), "Description");
                
                // Add cover image if provided
                if (model.CoverImage != null && model.CoverImage.Length > 0)
                {
                    using var fileStream = model.CoverImage.OpenReadStream();
                    var fileContent = new StreamContent(fileStream);
                    fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse(model.CoverImage.ContentType);
                    formData.Add(fileContent, "CoverImage", model.CoverImage.FileName);
                }
                
                var response = await client.PutAsync($"api/community/{communityId}", formData);
                
                if (response.IsSuccessStatusCode)
                {
                    TempData["Success"] = "Community updated successfully.";
                    return RedirectToAction("Details", new { communityId });
                }
                
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Error updating community. Status: {StatusCode}, Response: {Response}", 
                    response.StatusCode, errorContent);
                    
                ModelState.AddModelError("", "An error occurred while updating the community. Please try again later.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating community. CommunityId: {CommunityId}", communityId);
                ModelState.AddModelError("", "A system error occurred. Please try again later.");
            }
            
            // If we got this far, something failed, redisplay form
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CommunityViewModel model, IFormFile coverImage)
        {
            if (!User.Identity.IsAuthenticated)
            {
                _logger.LogWarning("Unauthenticated user attempted to create a community");
                return RedirectToAction("Login", "Auth");
            }

            var currentUserId = GetUserId();
            _logger.LogInformation("Starting community creation process for user {UserId}", currentUserId);

            ModelState.Remove("CreatedByUserID");
            _logger.LogDebug("Model state is valid: {IsValid}", ModelState.IsValid);

            if (!ModelState.IsValid)
            {
                var modelType = model.GetType();
                var properties = modelType.GetProperties();
                var modelValues = string.Join("\n", properties
                    .Select(p => $"{p.Name} = {p.GetValue(model) ?? "null"} (Type: {p.PropertyType.Name})"));

                _logger.LogWarning("Model state is invalid. Model values:\n{ModelValues}", modelValues);
                _logger.LogWarning("Validation errors: {Errors}",
                    string.Join("; ", ModelState
                        .Where(ms => ms.Value.Errors.Count > 0)
                        .SelectMany(ms => ms.Value.Errors
                            .Select(e => $"{ms.Key}: {e.ErrorMessage}"))));
                return View(model);
            }

            try
            {
                _logger.LogInformation("Current user ID: {UserId}", currentUserId);

                if (currentUserId <= 0)
                {
                    _logger.LogError("Invalid user ID when creating community");
                    ModelState.AddModelError(string.Empty, "Unable to identify user. Please log in again.");
                    return View(model);
                }

                using var client = CreateAuthenticatedClient();
                using var formData = new MultipartFormDataContent();

                _logger.LogInformation("Preparing form data for community creation");
                _logger.LogDebug("Name: {Name}, Description: {Description}, UserId: {UserId}",
                    model.Name, model.Description, currentUserId);

                var nameContent = new StringContent(model.Name ?? string.Empty);
                var descriptionContent = new StringContent(model.Description ?? string.Empty);

                formData.Add(nameContent, "Name");
                formData.Add(descriptionContent, "Description");

                if (model.PrivacyID.HasValue)
                {
                    var privacyIdContent = new StringContent(model.PrivacyID.Value.ToString());
                    formData.Add(privacyIdContent, "PrivacyID");
                }

                _logger.LogInformation("Form data content:" +
                                     $"\nName: {model.Name}" +
                                     $"\nDescription: {model.Description}" +
                                     $"\nPrivacyID: {model.PrivacyID}");

                if (coverImage != null && coverImage.Length > 0)
                {
                    try
                    {
                        _logger.LogInformation("Processing cover image. File name: {FileName}, Length: {Length} bytes, ContentType: {ContentType}",
                            coverImage.FileName, coverImage.Length, coverImage.ContentType);

                        var fileStream = coverImage.OpenReadStream();
                        var fileContent = new StreamContent(fileStream);
                        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse(coverImage.ContentType);
                        formData.Add(fileContent, "coverImage", coverImage.FileName);
                        _logger.LogDebug("Successfully added cover image to form data");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing cover image");
                        ModelState.AddModelError(string.Empty, "An error occurred while processing the cover image. Please try again.");
                        return View(model);
                    }
                }
                else
                {
                    _logger.LogInformation("No cover image provided");
                }

                _logger.LogInformation("Sending community creation request to API...");
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();

                var response = await client.PostAsync("/api/community", formData);

                stopwatch.Stop();
                _logger.LogInformation("API request completed in {ElapsedMilliseconds}ms with status: {StatusCode}",
                    stopwatch.ElapsedMilliseconds, response.StatusCode);

                var responseContent = await response.Content.ReadAsStringAsync();
                _logger.LogDebug("API response content: {ResponseContent}", responseContent);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Community created successfully. Processing response...");

                    try
                    {
                        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                        var createdCommunity = JsonSerializer.Deserialize<CommunityViewModel>(responseContent, options);

                        if (createdCommunity != null)
                        {
                            _logger.LogInformation("Successfully parsed community response. New community ID: {CommunityId}",
                                createdCommunity.CommunityID);

                            TempData["Success"] = "Community created successfully!";
                            return RedirectToAction(nameof(Details), new { communityId = createdCommunity.CommunityID });
                        }

                        _logger.LogError("Failed to deserialize created community from response. Response was empty or invalid.");
                        ModelState.AddModelError(string.Empty, "Unable to process response from server.");
                    }
                    catch (JsonException ex)
                    {
                        _logger.LogError(ex, "Error deserializing created community. Response: {Response}", responseContent);
                        ModelState.AddModelError(string.Empty, "Error processing data from server.");
                    }
                }
                else
                {
                    _logger.LogWarning("Failed to create community. Status: {StatusCode}, Response: {Response}",
                        response.StatusCode, responseContent);

                    string errorMessage;
                    try
                    {
                        using var errorDoc = JsonDocument.Parse(responseContent);
                        if (errorDoc.RootElement.TryGetProperty("message", out var messageProp) &&
                            messageProp.ValueKind == JsonValueKind.String)
                        {
                            errorMessage = messageProp.GetString() ?? "Có lỗi xảy ra khi tạo cộng đồng.";
                            _logger.LogDebug("Extracted error message from response: {ErrorMessage}", errorMessage);
                        }
                        else if (errorDoc.RootElement.TryGetProperty("title", out var titleProp) &&
                                 titleProp.ValueKind == JsonValueKind.String)
                        {
                            errorMessage = titleProp.GetString() ?? "Có lỗi xảy ra khi tạo cộng đồng.";
                            _logger.LogDebug("Extracted error title from response: {ErrorMessage}", errorMessage);
                        }
                        else
                        {
                            errorMessage = responseContent;
                            _logger.LogDebug("Using raw response as error message");
                        }
                    }
                    catch (JsonException ex)
                    {
                        _logger.LogError(ex, "Error parsing error response");
                        errorMessage = response.StatusCode == System.Net.HttpStatusCode.Unauthorized
                            ? "You need to log in to perform this action."
                            : "An error occurred while creating the community. Please try again later.";
                    }

                    ModelState.AddModelError(string.Empty, errorMessage);
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Network error while creating community. StatusCode: {StatusCode}", ex.StatusCode);
                ModelState.AddModelError(string.Empty, "Unable to connect to the server. Please check your network connection and try again.");
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Error deserializing response when creating community");
                ModelState.AddModelError(string.Empty, "Lỗi xử lý dữ liệu từ máy chủ.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error creating community");
                ModelState.AddModelError(string.Empty, "An unexpected error occurred while creating the community.");
            }

            _logger.LogInformation("Community creation process completed with errors");
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
                return RedirectToAction("Login", "Auth");

            var userId = GetUserId();
            ViewBag.CurrentUserId = userId;
            ViewBag.UserRole = "None"; // Default role

            try
            {
                using var client = CreateAuthenticatedClient();
                var response = await client.GetAsync($"api/community/{communityId}");

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var responseData = JsonSerializer.Deserialize<CommunityDetailsResponse>(content, options);

                    if (responseData != null)
                    {
                        // First try to get role from response
                        var userRole = responseData.UserRole ?? "None";
                        
                        // If role is None, try to get it from members list
                        if (userRole == "None" && responseData.Members != null)
                        {
                            var currentUserMember = responseData.Members
                                .FirstOrDefault(m => m.UserID == userId);
                                
                            if (currentUserMember != null)
                            {
                                userRole = currentUserMember.Role; // This will be "Admin" or "Member"
                                _logger.LogInformation("Set user role from members list: {Role}", userRole);
                            }
                        }
                        
                        ViewBag.UserRole = userRole;

                        var viewModel = new CommunityDetailsViewModel
                        {
                            Community = new CommunityViewModel
                            {
                                CommunityID = responseData.CommunityID,
                                Name = responseData.Name,
                                Description = responseData.Description,
                                CoverImageUrl = responseData.CoverImageUrl,
                                CreatedAt = responseData.CreatedAt,
                                UpdatedAt = responseData.UpdatedAt
                            },
                            Members = responseData.Members?.Select(m => new CommunityMemberViewModel
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
                            }).ToList() ?? new List<CommunityMemberViewModel>(),
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
                    return RedirectToAction("Login", "Auth");
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
            public string? CoverImageUrl { get; set; }
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