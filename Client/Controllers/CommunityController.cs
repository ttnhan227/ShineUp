using Client.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;

namespace Client.Controllers;

public class CommunityController : Controller
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<CommunityController> _logger;

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

        var token = HttpContext.Request.Cookies["auth_token"];

        if (string.IsNullOrEmpty(token) && User.Identity is ClaimsIdentity identity)
        {
            var jwtClaim = identity.FindFirst("JWT");
            token = jwtClaim?.Value;
        }

        if (!string.IsNullOrEmpty(token))
        {
            client.DefaultRequestHeaders.Remove("Authorization");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        client.DefaultRequestHeaders.Accept.Clear();
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        return client;
    }

    private int? GetUserId()
    {
        try
        {
            if (!User.Identity.IsAuthenticated)
            {
                return null;
            }

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
            {
                return null;
            }

            return userId;
        }
        catch
        {
            return null;
        }
    }

    [HttpGet]
    public IActionResult Create()
    {
        if (!User.Identity.IsAuthenticated)
        {
            return RedirectToAction("Login", "Auth");
        }

        var currentUserId = GetUserId();
        if (currentUserId == null || currentUserId <= 0)
        {
            return RedirectToAction("Login", "Auth");
        }

        _logger.LogInformation("Starting community creation process for user {UserId}", currentUserId);

        ModelState.Remove("CreatedByUserID");
        if (!ModelState.IsValid)
        {
            return View(new CommunityViewModel());
        }

        return View(new CommunityViewModel());
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> Edit([FromQuery] int communityId)
    {
        _logger.LogInformation("Fetching community details for editing. CommunityId: {CommunityId}", communityId);

        if (communityId <= 0)
        {
            return BadRequest("Invalid CommunityId");
        }

        try
        {
            using var client = CreateAuthenticatedClient();
            var response = await client.GetAsync($"api/community/{communityId}");
            var responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var community = JsonSerializer.Deserialize<CommunityViewModel>(responseContent, options);

                if (community != null)
                {
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
    public async Task<IActionResult> CreatePost(int communityId, [FromForm] string content, List<IFormFile> mediaFiles,
        int? privacyId = 1)
    {
        if (communityId <= 0)
        {
            return BadRequest("Invalid community ID.");
        }

        var currentUserId = GetUserId();
        if (currentUserId == null)
        {
            return Unauthorized("You need to be logged in to create a post.");
        }

        try
        {
            using var client = CreateAuthenticatedClient();
            using var formData = new MultipartFormDataContent();

            formData.Add(new StringContent(content), "Content");
            formData.Add(new StringContent(communityId.ToString()), "CommunityID");
            formData.Add(new StringContent(privacyId?.ToString() ?? "1"), "PrivacyID");

            // Add media files
            if (mediaFiles != null && mediaFiles.Any())
            {
                foreach (var file in mediaFiles)
                {
                    using var ms = new MemoryStream();
                    await file.CopyToAsync(ms);
                    var fileBytes = ms.ToArray();
                    formData.Add(new ByteArrayContent(fileBytes), "files", file.FileName);
                    formData.Add(new StringContent("image"), "MediaTypes"); // Assuming all files are images for now
                }
            }


            var response = await client.PostAsync("api/posts", formData);

            if (response.IsSuccessStatusCode)
            {
                return RedirectToAction("Details", new { communityId });
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            _logger.LogError("Failed to create post. Status: {StatusCode}, Response: {Response}",
                response.StatusCode, errorContent);

            TempData["Error"] = "Failed to create post. Please try again.";
            return RedirectToAction("Details", new { communityId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating post in community {CommunityId}", communityId);
            TempData["Error"] = "An error occurred while creating the post.";
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
            TempData["Error"] = "Invalid community ID.";
            return RedirectToAction("Details", new { communityId });
        }

        var currentUserId = GetUserId();
        if (currentUserId == null)
        {
            TempData["Error"] = "You need to be logged in to perform this action.";
            return RedirectToAction("Login", "Auth", new { returnUrl = $"/Community/Details/{communityId}" });
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            using var client = CreateAuthenticatedClient();
            using var formData = new MultipartFormDataContent();

            formData.Add(new StringContent(model.CommunityID.ToString()), "CommunityID");
            formData.Add(new StringContent(model.Name ?? ""), "Name");
            formData.Add(new StringContent(model.Description ?? ""), "Description");
            formData.Add(new StringContent(model.PrivacyID.ToString()), "PrivacyID");

            if (model.CoverImage != null)
            {
                using var ms = new MemoryStream();
                await model.CoverImage.CopyToAsync(ms);
                var fileBytes = ms.ToArray();
                formData.Add(new ByteArrayContent(fileBytes), "CoverImage", model.CoverImage.FileName);
            }

            var response = await client.PutAsync($"api/community/{communityId}", formData);

            if (response.IsSuccessStatusCode)
            {
                TempData["Success"] = "Community updated successfully.";
                return RedirectToAction("Details", new { communityId });
            }

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                TempData["Error"] = "Community not found.";
            }
            else if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                TempData["Error"] = "You are not authorized to update this community.";
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
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            if (userId <= 0)
            {
                ModelState.AddModelError(string.Empty, "Unable to identify user. Please log in again.");
                return View(model);
            }

            using var client = CreateAuthenticatedClient();
            using var formData = new MultipartFormDataContent();

            var nameContent = new StringContent(model.Name ?? string.Empty);
            var descriptionContent = new StringContent(model.Description ?? string.Empty);

            formData.Add(nameContent, "Name");
            formData.Add(descriptionContent, "Description");

            if (model.PrivacyID.HasValue)
            {
                var privacyIdContent = new StringContent(model.PrivacyID.Value.ToString());
                formData.Add(privacyIdContent, "PrivacyID");
            }

            if (coverImage != null && coverImage.Length > 0)
            {
                try
                {
                    var fileStream = coverImage.OpenReadStream();
                    var fileContent = new StreamContent(fileStream);
                    fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse(coverImage.ContentType);
                    formData.Add(fileContent, "coverImage", coverImage.FileName);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing cover image");
                    ModelState.AddModelError(string.Empty,
                        "An error occurred while processing the cover image. Please try again.");
                    return View(model);
                }
            }

            var response = await client.PostAsync("/api/community", formData);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                try
                {
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var createdCommunity = JsonSerializer.Deserialize<CommunityViewModel>(responseContent, options);

                    if (createdCommunity != null)
                    {
                        TempData["Success"] = "Community created successfully!";
                        return RedirectToAction(nameof(Details), new { communityId = createdCommunity.CommunityID });
                    }

                    ModelState.AddModelError(string.Empty, "Unable to process response from server.");
                }
                catch (JsonException)
                {
                    ModelState.AddModelError(string.Empty, "Error processing data from server.");
                }
            }
            else
            {
                string errorMessage;
                try
                {
                    using var errorDoc = JsonDocument.Parse(responseContent);
                    if (errorDoc.RootElement.TryGetProperty("message", out var messageProp) &&
                        messageProp.ValueKind == JsonValueKind.String)
                    {
                        errorMessage = messageProp.GetString() ?? "An error occurred while creating the community.";
                    }
                    else if (errorDoc.RootElement.TryGetProperty("title", out var titleProp) &&
                             titleProp.ValueKind == JsonValueKind.String)
                    {
                        errorMessage = titleProp.GetString() ?? "An error occurred while creating the community.";
                    }
                    else
                    {
                        errorMessage = responseContent;
                    }
                }
                catch
                {
                    errorMessage = response.StatusCode == HttpStatusCode.Unauthorized
                        ? "You need to log in to perform this action."
                        : "An error occurred while creating the community. Please try again later.";
                }

                ModelState.AddModelError(string.Empty, errorMessage);
            }
        }
        catch (HttpRequestException)
        {
            ModelState.AddModelError(string.Empty,
                "Unable to connect to the server. Please check your network connection and try again.");
        }
        catch (JsonException)
        {
            ModelState.AddModelError(string.Empty, "Error processing data from server.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error creating community");
            ModelState.AddModelError(string.Empty, "An unexpected error occurred while creating the community.");
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
            }

            TempData["Error"] = "Unable to retrieve the list of communities.";
            return View(new List<CommunityViewModel>());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in Index()");
            TempData["Error"] = "A system error occurred.";
            return View(new List<CommunityViewModel>());
        }
    }

    public async Task<IActionResult> Details(int communityId)
    {
        if (communityId <= 0)
        {
            return BadRequest("Invalid CommunityId");
        }

        _logger.LogInformation("Community Details requested. CommunityId: {CommunityId}", communityId);

        var userId = GetUserId();
        if (userId == null)
        {
            TempData["Error"] = "You need to be logged in to view this page.";
            return RedirectToAction("Login", "Auth", new { returnUrl = $"/Community/Details/{communityId}" });
        }

        ViewBag.CurrentUserId = userId.Value;
        ViewBag.UserRole = "None";
        var viewModel = new CommunityDetailsViewModel();

        try
        {
            using var client = CreateAuthenticatedClient();

            // Get community details
            var response = await client.GetAsync($"api/community/{communityId}");

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var responseObj = JsonDocument.Parse(content).RootElement;

                if (responseObj.ValueKind == JsonValueKind.Null)
                {
                    _logger.LogError("Received null response data from API");
                    TempData["Error"] = "Failed to load community details. Please try again later.";
                    return RedirectToAction("Index", "Home");
                }

                var community = new CommunityViewModel();
                if ((responseObj.TryGetProperty("CommunityID", out var communityIdProp) ||
                     responseObj.TryGetProperty("communityID", out communityIdProp)) &&
                    communityIdProp.ValueKind == JsonValueKind.Number)
                {
                    community.CommunityID = communityIdProp.GetInt32();
                }

                if ((responseObj.TryGetProperty("Name", out var nameProp) ||
                     responseObj.TryGetProperty("name", out nameProp)) &&
                    nameProp.ValueKind == JsonValueKind.String)
                {
                    community.Name = nameProp.GetString() ?? "Unknown Community";
                }

                if ((responseObj.TryGetProperty("Description", out var descProp) ||
                     responseObj.TryGetProperty("description", out descProp)) &&
                    descProp.ValueKind == JsonValueKind.String)
                {
                    community.Description = descProp.GetString() ?? "";
                }

                if ((responseObj.TryGetProperty("CoverImageUrl", out var imgProp) ||
                     responseObj.TryGetProperty("coverImageUrl", out imgProp)) &&
                    imgProp.ValueKind == JsonValueKind.String)
                {
                    community.CoverImageUrl = imgProp.GetString() ?? "/images/default-community-cover.jpg";
                }

                if ((responseObj.TryGetProperty("CreatedAt", out var createdAtProp) ||
                     responseObj.TryGetProperty("createdAt", out createdAtProp)) &&
                    createdAtProp.ValueKind == JsonValueKind.String &&
                    DateTime.TryParse(createdAtProp.GetString(), out var createdAt))
                {
                    community.CreatedAt = createdAt;
                }

                if (responseObj.TryGetProperty("CreatedByUserID", out var createdByIdProp) &&
                    createdByIdProp.ValueKind == JsonValueKind.Number)
                {
                    community.CreatedByUserID = createdByIdProp.GetInt32();
                }

                if (responseObj.TryGetProperty("privacyID", out var privacyIdProp) &&
                    privacyIdProp.ValueKind != JsonValueKind.Null)
                {
                    community.PrivacyID = privacyIdProp.GetInt32();
                }

                viewModel.Community = community;

                // Get community posts using the dedicated method
                viewModel.Posts = await GetCommunityPosts(client, communityId);

                // Get community members
                var members = new List<CommunityMemberViewModel>();
                if (responseObj.TryGetProperty("Members", out var membersElement) ||
                    responseObj.TryGetProperty("members", out membersElement))
                {
                    foreach (var member in membersElement.EnumerateArray())
                        try
                        {
                            var memberModel = new CommunityMemberViewModel
                            {
                                CommunityID = community.CommunityID,
                                Role = CommunityRole.Member,
                                User = new UserViewModel()
                            };

                            if (member.TryGetProperty("UserID", out var userIdProp) ||
                                member.TryGetProperty("userID", out userIdProp))
                            {
                                memberModel.UserID = userIdProp.GetInt32();
                            }

                            memberModel.Role = CommunityRole.Member;
                            if ((member.TryGetProperty("Role", out var roleProp) ||
                                 member.TryGetProperty("role", out roleProp)) &&
                                roleProp.ValueKind == JsonValueKind.String)
                            {
                                var roleStr = roleProp.GetString() ?? "";
                                if (roleStr.Equals("Moderator", StringComparison.OrdinalIgnoreCase) ||
                                    roleStr.Equals("1", StringComparison.OrdinalIgnoreCase) ||
                                    roleStr.Equals("true", StringComparison.OrdinalIgnoreCase))
                                {
                                    memberModel.Role = CommunityRole.Moderator;
                                }
                            }

                            if ((member.TryGetProperty("UserID", out var userIDProp) ||
                                 member.TryGetProperty("userID", out userIDProp)) &&
                                userIDProp.ValueKind == JsonValueKind.Number)
                            {
                                memberModel.User.UserID = userIDProp.GetInt32();
                            }

                            if ((member.TryGetProperty("Username", out var usernameProp) ||
                                 member.TryGetProperty("username", out usernameProp)) &&
                                usernameProp.ValueKind == JsonValueKind.String)
                            {
                                memberModel.User.Username = usernameProp.GetString() ?? string.Empty;
                            }

                            if ((member.TryGetProperty("FullName", out var fullNameProp) ||
                                 member.TryGetProperty("fullName", out fullNameProp)) &&
                                fullNameProp.ValueKind == JsonValueKind.String)
                            {
                                memberModel.User.FullName = fullNameProp.GetString() ?? "User";
                            }

                            if ((member.TryGetProperty("Email", out var emailProp) ||
                                 member.TryGetProperty("email", out emailProp)) &&
                                emailProp.ValueKind == JsonValueKind.String)
                            {
                                memberModel.User.Email = emailProp.GetString() ?? string.Empty;
                            }

                            if ((member.TryGetProperty("ProfileImageUrl", out var imgUrl) ||
                                 member.TryGetProperty("profileImageUrl", out imgUrl)) &&
                                imgUrl.ValueKind == JsonValueKind.String)
                            {
                                memberModel.User.ProfileImageURL = imgUrl.GetString() ?? string.Empty;
                            }

                            if ((member.TryGetProperty("JoinedAt", out var joinedAtProp) ||
                                 member.TryGetProperty("joinedAt", out joinedAtProp)) &&
                                joinedAtProp.ValueKind == JsonValueKind.String &&
                                DateTime.TryParse(joinedAtProp.GetString(), out var joinedDate))
                            {
                                memberModel.JoinedAt = joinedDate;
                            }

                            if ((member.TryGetProperty("LastActiveAt", out var lastActiveProp) ||
                                 member.TryGetProperty("lastActiveAt", out lastActiveProp)) &&
                                lastActiveProp.ValueKind == JsonValueKind.String &&
                                DateTime.TryParse(lastActiveProp.GetString(), out var lastActiveDate))
                            {
                                memberModel.LastActiveAt = lastActiveDate;
                            }

                            members.Add(memberModel);
                        }
                        catch
                        {
                            // Continue with next member
                        }
                }

                var currentUserId = GetUserId();
                var communityRole = CommunityRole.None;
                var currentMember = members.FirstOrDefault(m => m.UserID == currentUserId);
                if (currentMember != null)
                {
                    communityRole = currentMember.Role;
                }

                var isModerator = false;
                var isMember = false;
                if ((responseObj.TryGetProperty("IsCurrentUserModerator", out var moderatorElement) ||
                     responseObj.TryGetProperty("isCurrentUserModerator", out moderatorElement)) &&
                    moderatorElement.ValueKind == JsonValueKind.True)
                {
                    isModerator = true;
                    communityRole = CommunityRole.Moderator;
                }

                if ((responseObj.TryGetProperty("IsCurrentUserMember", out var memberElement) ||
                     responseObj.TryGetProperty("isCurrentUserMember", out memberElement)) &&
                    memberElement.ValueKind == JsonValueKind.True &&
                    communityRole == CommunityRole.None)
                {
                    isMember = true;
                    communityRole = CommunityRole.Member;
                }

                if (HttpContext.User.Identity is ClaimsIdentity existingIdentity &&
                    existingIdentity.IsAuthenticated)
                {
                    // User is already authenticated
                }

                viewModel.Members = members;
                viewModel.CommunityRole = communityRole;

                return View(viewModel);
            }

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                TempData["Error"] = "You need to be logged in to view this community.";
                return RedirectToAction("Login", "Auth");
            }

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                TempData["Error"] = "The requested community was not found.";
                return RedirectToAction("Index", "Community");
            }

            TempData["Error"] = "Failed to load community details. Please try again later.";
            return RedirectToAction("Index", "Home");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting community details for communityId={CommunityId}", communityId);
            TempData["Error"] = "An error occurred while loading the community. Please try again later.";
            return RedirectToAction("Index", "Home");
        }
    }

    private async Task<List<PostViewModel>> GetCommunityPosts(HttpClient client, int communityId)
    {
        try
        {
            var response = await client.GetAsync($"api/community/{communityId}/posts");
            _logger.LogInformation($"Posts API Response Status: {response.StatusCode}");

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning($"Failed to get posts. Status: {response.StatusCode}");
                return new List<PostViewModel>();
            }

            var content = await response.Content.ReadAsStringAsync();
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var posts = JsonSerializer.Deserialize<List<PostViewModel>>(content, options);
            _logger.LogInformation($"Successfully retrieved {posts?.Count ?? 0} posts");

            return posts ?? new List<PostViewModel>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving community posts");
            return new List<PostViewModel>();
        }
    }


    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Join(int communityId)
    {
        _logger.LogInformation("Join request received for community {CommunityId}", communityId);

        if (communityId <= 0)
        {
            return BadRequest("Invalid CommunityId");
        }

        var userId = GetUserId();
        if (userId == null)
        {
            TempData["Error"] = "You need to be logged in to join a community.";
            return RedirectToAction("Login", "Auth", new { returnUrl = $"/Community/Details/{communityId}" });
        }

        try
        {
            using var client = CreateAuthenticatedClient();
            var response = await client.PostAsync($"/api/community/{communityId}/join", null);

            if (response.IsSuccessStatusCode)
            {
                TempData["Success"] = "Successfully joined the community.";
                return RedirectToAction(nameof(Details), new { communityId });
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            TempData["Error"] = response.StatusCode == HttpStatusCode.BadRequest
                ? errorContent
                : "Unable to join the community. Please try again later.";

            return RedirectToAction(nameof(Details), new { communityId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error joining communityId={CommunityId}", communityId);
            TempData["Error"] = "A system error occurred.";
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
            return BadRequest("Invalid CommunityId");
        }

        var userId = GetUserId();
        if (userId == null)
        {
            TempData["Error"] = "You need to be logged in to leave a community.";
            return RedirectToAction("Login", "Auth", new { returnUrl = $"/Community/Details/{communityId}" });
        }

        try
        {
            using var client = CreateAuthenticatedClient();
            var response = await client.PostAsync($"api/community/{communityId}/leave", null);

            if (response.IsSuccessStatusCode)
            {
                TempData["Success"] = "Successfully left the community.";
                return RedirectToAction(nameof(Index));
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            TempData["Error"] = response.StatusCode == HttpStatusCode.BadRequest
                ? errorContent
                : "Unable to leave the community. Please try again later.";

            return RedirectToAction(nameof(Details), new { communityId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error leaving communityId={CommunityId}", communityId);
            TempData["Error"] = "A system error occurred.";
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
            TempData["Error"] = "Invalid request parameters.";
            return RedirectToAction("Details", new { communityId });
        }

        var currentUserId = GetUserId();
        if (currentUserId == null)
        {
            TempData["Error"] = "You need to be logged in to perform this action.";
            return RedirectToAction("Login", "Auth", new { returnUrl = $"/Community/Details/{communityId}" });
        }

        try
        {
            using var client = CreateAuthenticatedClient();
            var response = await client.DeleteAsync($"api/Community/{communityId}/members/{userId}");

            if (response.IsSuccessStatusCode)
            {
                TempData["Success"] = "Member removed successfully.";
            }
            else if (response.StatusCode == HttpStatusCode.Forbidden)
            {
                TempData["Error"] = "You don't have permission to remove members.";
            }
            else if (response.StatusCode == HttpStatusCode.NotFound)
            {
                TempData["Error"] = "Community or user not found.";
            }
            else
            {
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
        _logger.LogInformation(
            "TransferModerator request - CommunityId: {CommunityId}, NewModeratorId: {NewModeratorId}",
            communityId, newModeratorId);

        if (communityId <= 0 || newModeratorId <= 0)
        {
            TempData["Error"] = "Invalid request parameters.";
            // Fix: Use communityId instead of id in the route values
            return RedirectToAction("Details", new { communityId });
        }


        var currentUserId = GetUserId();
        if (currentUserId == null)
        {
            TempData["Error"] = "You need to be logged in to perform this action.";
            return RedirectToAction("Login", "Auth", new { returnUrl = $"/Community/Details/{communityId}" });
        }

        try
        {
            using var client = CreateAuthenticatedClient();
            var response =
                await client.PostAsync(
                    $"api/community/{communityId}/transfer-moderator?newModeratorId={newModeratorId}", null);

            if (response.IsSuccessStatusCode)
            {
                TempData["Success"] = "Moderator role transferred successfully.";
            }
            else if (response.StatusCode == HttpStatusCode.Forbidden)
            {
                TempData["Error"] = "You don't have permission to transfer moderator role.";
            }
            else if (response.StatusCode == HttpStatusCode.NotFound)
            {
                TempData["Error"] = "Community or user not found.";
            }
            else
            {
                TempData["Error"] = "Failed to transfer moderator role. Please try again.";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error transferring moderator for community {CommunityId}", communityId);
            TempData["Error"] = "An error occurred while transferring moderator role.";
        }

        return RedirectToAction("Details", new { communityId });
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
}