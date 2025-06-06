using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Security.Claims;
using System.Dynamic;
using Client.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using System.IO;
using System.Net.Http;
using System.Net;
using System.Linq;

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
            var client = _httpClientFactory.CreateClient("API");
            
            // Try to get token from cookies first
            var token = HttpContext.Request.Cookies["auth_token"];
            
            // If not found in cookies, try to get it from the JWT claim
            if (string.IsNullOrEmpty(token) && User.Identity is ClaimsIdentity identity)
            {
                var jwtClaim = identity.FindFirst("JWT");
                if (jwtClaim != null)
                {
                    token = jwtClaim.Value;
                    _logger.LogInformation("Retrieved JWT token from claims");
                }
            }

            if (!string.IsNullOrEmpty(token))
            {
                // Remove any existing Authorization header
                client.DefaultRequestHeaders.Remove("Authorization");
                // Add the new token
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                _logger.LogInformation("JWT token added to request headers");
            }
            else
            {
                _logger.LogWarning("JWT token not found in cookies or claims");
            }

            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            return client;
        }

        private int? GetUserId()
        {
            try
            {
                _logger.LogInformation("Getting user ID from claims. All claims:");
                
                if (!User.Identity.IsAuthenticated)
                {
                    _logger.LogWarning("User is not authenticated");
                    return null;
                }

                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null)
                {
                    _logger.LogWarning("NameIdentifier claim not found");
                    return null;
                }

                _logger.LogInformation($"Found NameIdentifier claim: {userIdClaim.Value}");
                
                if (int.TryParse(userIdClaim.Value, out int userId))
                {
                    _logger.LogInformation($"Successfully parsed user ID: {userId}");
                    return userId;
                }
                
                _logger.LogWarning($"Failed to parse user ID from claim value: {userIdClaim.Value}");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user ID from claims");
                return null;
            }
        }

        [HttpGet]
        public IActionResult Create()
        {
            if (!User.Identity.IsAuthenticated)
            {
                _logger.LogWarning("Unauthenticated user attempted to create a community");
                return RedirectToAction("Login", "Auth");
            }

            var currentUserId = GetUserId();
            if (currentUserId == null || currentUserId <= 0)
            {
                _logger.LogWarning("User not authenticated or invalid user ID");
                return RedirectToAction("Login", "Auth");
            }

            _logger.LogInformation("Starting community creation process for user {UserId}", currentUserId);

            ModelState.Remove("CreatedByUserID");
            _logger.LogDebug("Model state is valid: {IsValid}", ModelState.IsValid);

            if (!ModelState.IsValid)
            {
                var modelType = typeof(CommunityViewModel);
                var properties = modelType.GetProperties();
                var modelValues = string.Join("\n", properties
                    .Select(p => $"{p.Name} = {p.GetValue(null) ?? "null"} (Type: {p.PropertyType.Name})"));

                _logger.LogWarning("Model state is invalid. Model values:\n{ModelValues}", modelValues);
                _logger.LogWarning("Validation errors: {Errors}",
                    string.Join("; ", ModelState
                        .Where(ms => ms.Value.Errors.Count > 0)
                        .SelectMany(ms => ms.Value.Errors
                            .Select(e => $"{ms.Key}: {e.ErrorMessage}"))));
                return View(new CommunityViewModel());
            }

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
            _logger.LogInformation("Edit community request received - CommunityId: {CommunityId}", communityId);
            
            if (communityId <= 0 || communityId != model.CommunityID)
            {
                _logger.LogWarning("Invalid community ID in request - Path: {PathId}, Model: {ModelId}", 
                    communityId, model.CommunityID);
                TempData["Error"] = "Invalid community ID.";
                return RedirectToAction("Details", new { communityId });
            }

            var currentUserId = GetUserId();
            if (currentUserId == null)
            {
                _logger.LogWarning("User not authenticated. Redirecting to login.");
                TempData["Error"] = "You need to be logged in to perform this action.";
                return RedirectToAction("Login", "Auth", new { returnUrl = $"/Community/Details/{communityId}" });
            }
            
            _logger.LogInformation("User {UserId} attempting to edit community {CommunityId}", 
                currentUserId, communityId);
                
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid model state. Errors: {Errors}", 
                    string.Join("; ", ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)));
                
                // Log model state for debugging
                var modelType = typeof(EditCommunityViewModel);
                var properties = modelType.GetProperties();
                var modelValues = string.Join("\n", properties
                    .Select(p => $"{p.Name} = {p.GetValue(model) ?? "null"} (Type: {p.PropertyType.Name})"));

                _logger.LogWarning("Model values:\n{ModelValues}", modelValues);
                
                return View(model);
            }

            try
            {
                using var client = CreateAuthenticatedClient();
                _logger.LogInformation("Created authenticated client for community update");

                // Create a new FormData object to handle file upload
                using var formData = new MultipartFormDataContent();

                // Add the model properties to the form data
                formData.Add(new StringContent(model.CommunityID.ToString()), "CommunityID");
                formData.Add(new StringContent(model.Name ?? ""), "Name");
                formData.Add(new StringContent(model.Description ?? ""), "Description");
                formData.Add(new StringContent(model.PrivacyID.ToString()), "PrivacyID");

                _logger.LogInformation("Added basic fields to form data");

                // Handle file upload if a new image is provided
                if (model.CoverImage != null)
                {
                    _logger.LogInformation("Processing cover image upload");
                    using var ms = new MemoryStream();
                    await model.CoverImage.CopyToAsync(ms);
                    var fileBytes = ms.ToArray();
                    formData.Add(new ByteArrayContent(fileBytes), "CoverImage", model.CoverImage.FileName);
                    _logger.LogInformation("Added cover image to form data - Size: {Size} bytes, Name: {FileName}", 
                        fileBytes.Length, model.CoverImage.FileName);
                }
                else
                {
                    _logger.LogInformation("No new cover image provided");
                }

                _logger.LogInformation("Sending PUT request to update community");
                var response = await client.PutAsync($"api/community/{communityId}", formData);
                _logger.LogInformation("Received response with status: {StatusCode}", response.StatusCode);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Community updated successfully");
                    TempData["Success"] = "Community updated successfully.";
                    return RedirectToAction("Details", new { communityId });
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to update community. Status: {StatusCode}, Response: {Response}", 
                    response.StatusCode, errorContent);

                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    TempData["Error"] = "Community not found.";
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    TempData["Error"] = "You are not authorized to update this community.";
                    _logger.LogWarning("User {UserId} is not authorized to update community {CommunityId}", 
                        currentUserId, communityId);
                }
                else
                {
                    TempData["Error"] = "An error occurred while updating the community. Please try again.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating community {CommunityId}", communityId);
                TempData["Error"] = "An error occurred while updating the community. Please try again.";
            }

            // If we got this far, something failed, redisplay form with current values
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CommunityViewModel model, IFormFile coverImage)
        {
            var userId = GetUserId();
            if (userId == null)
            {
                TempData["Error"] = "You need to be logged in to create a community.";
                return RedirectToAction("Login", "Auth");
            }

            _logger.LogInformation("Starting community creation process for user {UserId}", userId);

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
                _logger.LogInformation("Current user ID: {UserId}", userId);

                if (userId <= 0)
                {
                    _logger.LogError("Invalid user ID when creating community");
                    ModelState.AddModelError(string.Empty, "Unable to identify user. Please log in again.");
                    return View(model);
                }

                using var client = CreateAuthenticatedClient();
                using var formData = new MultipartFormDataContent();

                _logger.LogInformation("Preparing form data for community creation");
                _logger.LogDebug("Name: {Name}, Description: {Description}, UserId: {UserId}",
                    model.Name, model.Description, userId);

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

    _logger.LogInformation("Community Details requested. CommunityId: {CommunityId}", communityId);
    
    var userId = GetUserId();
    if (userId == null)
    {
        _logger.LogWarning("User not authenticated. Redirecting to login.");
        TempData["Error"] = "You need to be logged in to view this page.";
        return RedirectToAction("Login", "Auth", new { returnUrl = $"/Community/Details/{communityId}" });
    }

    _logger.LogInformation("Current User ID: {UserId}", userId.Value);
    ViewBag.CurrentUserId = userId.Value;
    ViewBag.UserRole = "None"; // Default role
    
    // Log authentication state
    _logger.LogInformation("User authenticated: {IsAuthenticated}, Name: {Name}", 
        User.Identity?.IsAuthenticated, User.Identity?.Name);
    _logger.LogInformation("Authentication type: {AuthType}", User.Identity?.AuthenticationType);
    _logger.LogInformation("Claims count: {ClaimsCount}", User.Claims.Count());
    foreach (var claim in User.Claims)
    {
        _logger.LogInformation("Claim: {Type} = {Value}", claim.Type, claim.Value);
    }

    try
    {
        using var client = CreateAuthenticatedClient();
        var response = await client.GetAsync($"api/community/{communityId}");

        if (response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            _logger.LogInformation("Raw API response: {Response}", content);
            
            try
            {
                var responseObj = JsonDocument.Parse(content).RootElement;
                
                if (responseObj.ValueKind == JsonValueKind.Null)
                {
                    _logger.LogError("Received null response data from API");
                    TempData["Error"] = "Failed to load community details. Please try again later.";
                    return RedirectToAction("Index", "Home");
                }

                // Log all top-level properties for debugging
                _logger.LogInformation("Available properties in response: {Properties}", 
                    string.Join(", ", responseObj.EnumerateObject().Select(p => p.Name)));

                // Map community properties with both casings
                var community = new CommunityViewModel();
                
                // Map properties with both casings
                if ((responseObj.TryGetProperty("CommunityID", out var communityIdProp) || responseObj.TryGetProperty("communityID", out communityIdProp)) && 
                    communityIdProp.ValueKind == JsonValueKind.Number)
                    community.CommunityID = communityIdProp.GetInt32();
                    
                if ((responseObj.TryGetProperty("Name", out var nameProp) || responseObj.TryGetProperty("name", out nameProp)) && 
                    nameProp.ValueKind == JsonValueKind.String)
                    community.Name = nameProp.GetString() ?? "Unknown Community";
                    
                if ((responseObj.TryGetProperty("Description", out var descProp) || responseObj.TryGetProperty("description", out descProp)) && 
                    descProp.ValueKind == JsonValueKind.String)
                    community.Description = descProp.GetString() ?? "";
                    
                if ((responseObj.TryGetProperty("CoverImageUrl", out var imgProp) || responseObj.TryGetProperty("coverImageUrl", out imgProp)) && 
                    imgProp.ValueKind == JsonValueKind.String)
                    community.CoverImageUrl = imgProp.GetString() ?? "/images/default-community-cover.jpg";
                    
                if ((responseObj.TryGetProperty("CreatedAt", out var createdAtProp) || responseObj.TryGetProperty("createdAt", out createdAtProp)) && 
                    createdAtProp.ValueKind == JsonValueKind.String && 
                    DateTime.TryParse(createdAtProp.GetString(), out var createdAt))
                    community.CreatedAt = createdAt;
                    
                if (responseObj.TryGetProperty("CreatedByUserID", out var createdByIdProp) && 
                    createdByIdProp.ValueKind == JsonValueKind.Number)
                    community.CreatedByUserID = createdByIdProp.GetInt32();
                    
                if (responseObj.TryGetProperty("privacyID", out var privacyIdProp) && privacyIdProp.ValueKind != JsonValueKind.Null)
                    community.PrivacyID = privacyIdProp.GetInt32();

                // Map members
                var members = new List<CommunityMemberViewModel>();
                if (responseObj.TryGetProperty("Members", out var membersElement) || 
                    responseObj.TryGetProperty("members", out membersElement))
                {
                    if (membersElement.ValueKind != JsonValueKind.Array)
                    {
                        _logger.LogWarning("Members property is not an array");
                    }
                    _logger.LogInformation("Found {Count} members in the community", membersElement.GetArrayLength());
                    
                    foreach (var member in membersElement.EnumerateArray())
                    {
                        try
                        {
                            var memberModel = new CommunityMemberViewModel
                            {
                                CommunityID = community.CommunityID,
                                Role = CommunityRole.Member, // Default role
                                User = new UserViewModel()
                            };
                            
                            // Map member properties with both casings
                            if (member.TryGetProperty("UserID", out var userIdProp) || member.TryGetProperty("userID", out userIdProp))
                                memberModel.UserID = userIdProp.GetInt32();
                            
                            // Map role with both casings and check for moderator role
                            memberModel.Role = CommunityRole.Member; // Default to Member
                            
                            if ((member.TryGetProperty("Role", out var roleProp) || member.TryGetProperty("role", out roleProp)) && 
                                roleProp.ValueKind == JsonValueKind.String)
                            {
                                var roleStr = roleProp.GetString() ?? "";
                                _logger.LogInformation("Raw role value: {RoleValue}", roleStr);
                                
                                if (roleStr.Equals("Moderator", StringComparison.OrdinalIgnoreCase) || 
                                    roleStr.Equals("1", StringComparison.OrdinalIgnoreCase) ||
                                    roleStr.Equals("true", StringComparison.OrdinalIgnoreCase))
                                {
                                    memberModel.Role = CommunityRole.Moderator;
                                    _logger.LogInformation("Set role to Moderator for user {UserId}", memberModel.UserID);
                                }
                            }
                            
                            // Map user properties with both casings
                            if ((member.TryGetProperty("UserID", out var userIDProp) || member.TryGetProperty("userID", out userIDProp)) && 
                                userIDProp.ValueKind == JsonValueKind.Number)
                                memberModel.User.UserID = userIDProp.GetInt32();
                            
                            // Map username with both casings
                            if ((member.TryGetProperty("Username", out var usernameProp) || member.TryGetProperty("username", out usernameProp)) && 
                                usernameProp.ValueKind == JsonValueKind.String)
                                memberModel.User.Username = usernameProp.GetString() ?? string.Empty;
                            
                            // Map full name with both casings
                            if ((member.TryGetProperty("FullName", out var fullNameProp) || member.TryGetProperty("fullName", out fullNameProp)) && 
                                fullNameProp.ValueKind == JsonValueKind.String)
                                memberModel.User.FullName = fullNameProp.GetString() ?? "User";
                            
                            // Map email with both casings
                            if ((member.TryGetProperty("Email", out var emailProp) || member.TryGetProperty("email", out emailProp)) && 
                                emailProp.ValueKind == JsonValueKind.String)
                                memberModel.User.Email = emailProp.GetString() ?? string.Empty;
                            
                            // Map profile image URL with both casings
                            if ((member.TryGetProperty("ProfileImageUrl", out var imgUrl) || member.TryGetProperty("profileImageUrl", out imgUrl)) && 
                                imgUrl.ValueKind == JsonValueKind.String)
                                memberModel.User.ProfileImageURL = imgUrl.GetString() ?? string.Empty;
                            
                            // Map dates with both casings
                            if ((member.TryGetProperty("JoinedAt", out var joinedAtProp) || member.TryGetProperty("joinedAt", out joinedAtProp)) && 
                                joinedAtProp.ValueKind == JsonValueKind.String && 
                                DateTime.TryParse(joinedAtProp.GetString(), out var joinedDate))
                                memberModel.JoinedAt = joinedDate;
                            
                            if ((member.TryGetProperty("LastActiveAt", out var lastActiveProp) || member.TryGetProperty("lastActiveAt", out lastActiveProp)) && 
                                lastActiveProp.ValueKind == JsonValueKind.String && 
                                DateTime.TryParse(lastActiveProp.GetString(), out var lastActiveDate))
                                memberModel.LastActiveAt = lastActiveDate;
                            
                            members.Add(memberModel);
                            _logger.LogInformation("Successfully mapped member - Username: {Username}, ID: {UserId}, Role: {Role}", 
                                memberModel.User.Username, memberModel.UserID, memberModel.Role);
                            _logger.LogInformation("Member details - FullName: {FullName}, Email: {Email}, Image: {Image}",
                                memberModel.User.FullName, memberModel.User.Email, memberModel.User.ProfileImageURL);
                        }
                        catch (Exception memberEx)
                        {
                            _logger.LogWarning(memberEx, "Error processing member in community {CommunityId}", community.CommunityID);
                            // Continue with next member
                        }
                    }
                }


                // Get the current user ID
                var currentUserId = GetUserId();
                _logger.LogInformation("Current user ID: {UserId}", currentUserId);
                
                // Default role is None
                var communityRole = CommunityRole.None;
                
                // First check the member list for the current user
                var currentMember = members.FirstOrDefault(m => m.UserID == currentUserId);
                if (currentMember != null)
                {
                    _logger.LogInformation("User {UserId} found in members list with role: {Role}", currentUserId, currentMember.Role);
                    communityRole = currentMember.Role; // Use the role from the member list
                }
                
                // Then check the API flags for additional verification
                bool isModerator = false;
                bool isMember = false;
                
                if ((responseObj.TryGetProperty("IsCurrentUserModerator", out var moderatorElement) || 
                     responseObj.TryGetProperty("isCurrentUserModerator", out moderatorElement)) &&
                    moderatorElement.ValueKind == JsonValueKind.True)
                {
                    isModerator = true;
                    communityRole = CommunityRole.Moderator; // Override with moderator if API says so
                }
                
                if ((responseObj.TryGetProperty("IsCurrentUserMember", out var memberElement) || 
                     responseObj.TryGetProperty("isCurrentUserMember", out memberElement)) &&
                    memberElement.ValueKind == JsonValueKind.True && 
                    communityRole == CommunityRole.None) // Only set to member if not already a moderator
                {
                    isMember = true;
                    communityRole = CommunityRole.Member;
                }
                
                _logger.LogInformation("Final role determination - IsModerator: {IsModerator}, IsMember: {IsMember}, FinalRole: {FinalRole}", 
                    isModerator, isMember, communityRole);
                    
                // Log detailed membership info
                _logger.LogInformation("Current member details - Found: {Found}, Role: {Role}", 
                    currentMember != null, currentMember?.Role);
                    
                _logger.LogInformation("Assigned community role: {Role}", communityRole);

                // No need to sign in again - we already have the authentication state
                // Just ensure the current user context is properly set
                if (HttpContext.User.Identity is ClaimsIdentity existingIdentity && 
                    existingIdentity.IsAuthenticated)
                {
                    _logger.LogInformation("User is already authenticated. No need to re-authenticate.");
                }
                else
                {
                    _logger.LogWarning("User is not properly authenticated. Authentication may be required.");
                }

                // Create the view model
                var viewModel = new CommunityDetailsViewModel
                {
                    Community = community,
                    Members = members,
                    CommunityRole = communityRole
                };

                // Debug log the view model
                _logger.LogInformation("View model created - Community: {CommunityId}, MemberCount: {MemberCount}, Role: {Role}",
                    community.CommunityID, members.Count, communityRole);
                    
                // Log first few members for debugging
                foreach (var member in members.Take(3))
                {
                    _logger.LogInformation("Member in view model - ID: {Id}, Name: {Name}, Role: {Role}",
                        member.UserID, member.User?.FullName, member.Role);
                }

                // Set ViewBag properties for backward compatibility
                var userRole = communityRole.ToString();
                var communityIdValue = communityId;
                
                ViewBag.UserRole = userRole;
                ViewBag.CurrentUserId = currentUserId; // Using the one from JWT token
                ViewBag.CommunityId = communityIdValue;
                
                _logger.LogInformation("ViewBag set - UserRole: {UserRole}, UserId: {UserId}, CommunityId: {CommunityId}",
                    userRole, currentUserId, communityIdValue);

                return View(viewModel);
            }
            catch (JsonException jsonEx)
            {
                _logger.LogError(jsonEx, "Failed to deserialize community details");
                TempData["Error"] = "Invalid data received from the server.";
                return RedirectToAction("Index", "Home");
            }
        }
        else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            TempData["Error"] = "You need to be logged in to view this community.";
            return RedirectToAction("Login", "Auth");
        }
        else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            TempData["Error"] = "The requested community was not found.";
            return RedirectToAction("Index", "Community");
        }
        else
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            _logger.LogWarning("Failed to get community details. Status: {StatusCode}, Response: {Response}", 
                response.StatusCode, errorContent);
            TempData["Error"] = "Failed to load community details. Please try again later.";
            return RedirectToAction("Index", "Home");
        }
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error getting community details for communityId={CommunityId}", communityId);
        TempData["Error"] = "An error occurred while loading the community. Please try again later.";
        return RedirectToAction("Index", "Home");
    }
}

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Join(int communityId)
        {
            _logger.LogInformation("Join request received for community {CommunityId}", communityId);
            
            if (communityId <= 0) 
            {
                _logger.LogWarning("Invalid CommunityId: {CommunityId}", communityId);
                return BadRequest("CommunityId không hợp lệ");
            }

            var userId = GetUserId();
            if (userId == null)
            {
                _logger.LogWarning("User not authenticated. Redirecting to login.");
                TempData["Error"] = "You need to be logged in to join a community.";
                return RedirectToAction("Login", "Auth", new { returnUrl = $"/Community/Details/{communityId}" });
            }
            
            _logger.LogInformation("User {UserId} attempting to join community {CommunityId}", userId, communityId);

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
            _logger.LogInformation("Leave request received for community {CommunityId}", communityId);
            
            if (communityId <= 0)
            {
                _logger.LogWarning("Invalid CommunityId: {CommunityId}", communityId);
                return BadRequest("CommunityId không hợp lệ");
            }

            var userId = GetUserId();
            if (userId == null)
            {
                _logger.LogWarning("User not authenticated. Redirecting to login.");
                TempData["Error"] = "You need to be logged in to leave a community.";
                return RedirectToAction("Login", "Auth", new { returnUrl = $"/Community/Details/{communityId}" });
            }
            
            _logger.LogInformation("User {UserId} attempting to leave community {CommunityId}", userId, communityId);

            try
            {
                using var client = CreateAuthenticatedClient();
                var response = await client.PostAsync($"api/community/{communityId}/leave", null);

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
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveMember(int communityId, int userId)
        {
            _logger.LogInformation("RemoveMember request - CommunityId: {CommunityId}, TargetUserId: {TargetUserId}", 
                communityId, userId);
                
            if (communityId <= 0 || userId <= 0)
            {
                _logger.LogWarning("Invalid parameters - CommunityId: {CommunityId}, UserId: {UserId}", 
                    communityId, userId);
                TempData["Error"] = "Invalid request parameters.";
                return RedirectToAction("Details", new { communityId });
            }
            
            var currentUserId = GetUserId();
            if (currentUserId == null)
            {
                _logger.LogWarning("User not authenticated. Redirecting to login.");
                TempData["Error"] = "You need to be logged in to perform this action.";
                return RedirectToAction("Login", "Auth", new { returnUrl = $"/Community/Details/{communityId}" });
            }
            
            _logger.LogInformation("User {CurrentUserId} attempting to remove user {TargetUserId} from community {CommunityId}", 
                currentUserId, userId, communityId);

            try
            {
                using var client = CreateAuthenticatedClient();
                var response = await client.DeleteAsync($"api/Community/{communityId}/members/{userId}");

                if (response.IsSuccessStatusCode)
                {
                    TempData["Success"] = "Member removed successfully.";
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
                {
                    TempData["Error"] = "You don't have permission to remove members.";
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    TempData["Error"] = "Community or user not found.";
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Error removing member. Status: {StatusCode}, Response: {Response}", 
                        response.StatusCode, errorContent);
                    TempData["Error"] = "Failed to remove member. Please try again.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing member from community {CommunityId}", communityId);
                TempData["Error"] = "An error occurred while removing the member.";
            }

            return RedirectToAction("Details", new { communityId });
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TransferModerator(int communityId, int newModeratorId)
        {
            _logger.LogInformation("TransferModerator request - CommunityId: {CommunityId}, NewModeratorId: {NewModeratorId}", 
                communityId, newModeratorId);
                
            if (communityId <= 0 || newModeratorId <= 0)
            {
                _logger.LogWarning("Invalid parameters - CommunityId: {CommunityId}, NewModeratorId: {NewModeratorId}", 
                    communityId, newModeratorId);
                TempData["Error"] = "Invalid request parameters.";
                return RedirectToAction("Details", new { id = communityId });
            }
            
            var currentUserId = GetUserId();
            if (currentUserId == null)
            {
                _logger.LogWarning("User not authenticated. Redirecting to login.");
                TempData["Error"] = "You need to be logged in to perform this action.";
                return RedirectToAction("Login", "Auth", new { returnUrl = $"/Community/Details/{communityId}" });
            }
            
            _logger.LogInformation("User {CurrentUserId} attempting to transfer moderator role to user {NewModeratorId} in community {CommunityId}", 
                currentUserId, newModeratorId, communityId);

            try
            {
                using var client = CreateAuthenticatedClient();
                var response = await client.PostAsync($"api/community/{communityId}/transfer-moderator?newModeratorId={newModeratorId}", null);

                if (response.IsSuccessStatusCode)
                {
                    TempData["Success"] = "Moderator role transferred successfully.";
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
                {
                    TempData["Error"] = "You don't have permission to transfer moderator role.";
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    TempData["Error"] = "Community or user not found.";
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Error transferring moderator. Status: {StatusCode}, Response: {Response}", 
                        response.StatusCode, errorContent);
                    TempData["Error"] = "Failed to transfer moderator role. Please try again.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error transferring moderator for community {CommunityId}", communityId);
                TempData["Error"] = "An error occurred while transferring moderator role.";
            }

            return RedirectToAction("Details", new { communityId });        }

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