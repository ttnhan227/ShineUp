using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Client.Models;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Net;
using System.Security.Claims;

namespace Client.Controllers;

public class CommunityController : Controller
{
    private readonly IHttpClientFactory _clientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<CommunityController> _logger;

    public CommunityController(
        IHttpClientFactory clientFactory,
        IConfiguration configuration,
        ILogger<CommunityController> logger)
    {
        _clientFactory = clientFactory;
        _configuration = configuration;
        _logger = logger;
    }

    // GET: Community
    public async Task<IActionResult> Index()
    {
        try
        {
            var client = _clientFactory.CreateClient("API");
            var token = HttpContext.Request.Cookies["auth_token"];
            
            if (!string.IsNullOrEmpty(token))
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            var response = await client.GetAsync("api/community");
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to get communities. Status code: {StatusCode}", response.StatusCode);
                return View("Error");
            }

            var content = await response.Content.ReadAsStringAsync();
            var communities = JsonSerializer.Deserialize<List<CommunityViewModel>>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return View(communities);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting communities");
            return View("Error");
        }
    }

    // GET: Community/Details/5
    public async Task<IActionResult> Details(int id)
    {
        try
        {
            var client = _clientFactory.CreateClient("API");
            var token = HttpContext.Request.Cookies["auth_token"];
            var currentUserId = GetCurrentUserId();
            
            if (!string.IsNullOrEmpty(token))
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            // Get community details
            var communityResponse = await client.GetAsync($"api/community/{id}");
            if (!communityResponse.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to get community {CommunityId}. Status code: {StatusCode}", id, communityResponse.StatusCode);
                return NotFound();
            }

            var communityContent = await communityResponse.Content.ReadAsStringAsync();
            var community = JsonSerializer.Deserialize<CommunityDetailsViewModel>(communityContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            // Get community members
            var members = new List<CommunityMemberViewModel>();
            var membersResponse = await client.GetAsync($"api/community/{id}/members");
            if (membersResponse.IsSuccessStatusCode)
            {
                var membersContent = await membersResponse.Content.ReadAsStringAsync();
                members = JsonSerializer.Deserialize<List<CommunityMemberViewModel>>(membersContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }) ?? new List<CommunityMemberViewModel>();
            }

            // Get community posts
            var posts = new List<PostViewModel>();
            var postsResponse = await client.GetAsync($"api/community/{id}/posts");
            if (postsResponse.IsSuccessStatusCode)
            {
                var postsContent = await postsResponse.Content.ReadAsStringAsync();
                posts = JsonSerializer.Deserialize<List<PostViewModel>>(postsContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }) ?? new List<PostViewModel>();
            }

            // Determine user's role in the community
            var userRole = GetUserRoleInCommunity(currentUserId, community, members);
            
            // Set ViewBag data for the view
            ViewBag.Members = members;
            ViewBag.Posts = posts;
            ViewBag.UserRole = userRole;
            ViewBag.CurrentUserId = currentUserId;
            ViewBag.IsAdmin = userRole == UserCommunityRole.Admin;
            ViewBag.IsMember = userRole == UserCommunityRole.Member || userRole == UserCommunityRole.Admin;
            ViewBag.CanJoin = userRole == UserCommunityRole.NonMember && User.Identity.IsAuthenticated;
            ViewBag.CanLeave = userRole == UserCommunityRole.Member;
            ViewBag.CanCreatePost = userRole == UserCommunityRole.Member || userRole == UserCommunityRole.Admin;

            return View(community);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting community details for {CommunityId}", id);
            return View("Error");
        }
    }

    // GET: Community/Create
    [Authorize]
    public async Task<IActionResult> Create()
    {
        try
        {
            var client = _clientFactory.CreateClient("API");
            var viewModel = new CreateCommunityViewModel();

            // Get privacy options
            var privacyResponse = await client.GetAsync("api/community/privacy-options");
            if (privacyResponse.IsSuccessStatusCode)
            {
                var privacyContent = await privacyResponse.Content.ReadAsStringAsync();
                var privacyOptions = JsonSerializer.Deserialize<List<PrivacyViewModel>>(privacyContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                viewModel.PrivacyOptions = new SelectList(privacyOptions, "PrivacyID", "Name");
            }

            return View(viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading create community form data");
            return View("Error");
        }
    }

    // POST: Community/Create
    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([FromForm] CreateCommunityViewModel model)
    {
        if (!ModelState.IsValid)
        {
            _logger.LogWarning("CreateCommunity ModelState is invalid.");
            foreach (var state in ModelState.Values)
            {
                foreach (var error in state.Errors)
                {
                    _logger.LogError($"ModelState error: {error.ErrorMessage}");
                }
            }
            await LoadPrivacyOptions(model);
            return View(model);
        }

        try
        {
            var jwtToken = User.FindFirst("JWT")?.Value;
            if (string.IsNullOrEmpty(jwtToken))
            {
                _logger.LogError("JWT token is missing");
                return RedirectToAction("Login", "Auth", new { returnUrl = Url.Action("Create", "Community") });
            }

            var client = _clientFactory.CreateClient("API");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwtToken);

            var formData = new MultipartFormDataContent();
            formData.Add(new StringContent(model.Name), "Name");
            
            if (!string.IsNullOrWhiteSpace(model.Description))
            {
                formData.Add(new StringContent(model.Description), "Description");
            }
            
            formData.Add(new StringContent((model.PrivacyID ?? 1).ToString()), "PrivacyID");

            // Handle cover image upload
            if (model.CoverImage != null && model.CoverImage.Length > 0)
            {
                var imageContent = new StreamContent(model.CoverImage.OpenReadStream());
                imageContent.Headers.ContentType = new MediaTypeHeaderValue(model.CoverImage.ContentType);
                formData.Add(imageContent, "CoverImage", model.CoverImage.FileName);
            }

            _logger.LogInformation($"Creating community with Name: {model.Name}, PrivacyID: {model.PrivacyID}");

            var response = await client.PostAsync("api/community", formData);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Community created successfully");
                return RedirectToAction(nameof(Index));
            }

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                _logger.LogWarning("Token expired or invalid, redirecting to login");
                return RedirectToAction("Login", "Auth", new { returnUrl = Url.Action("Create", "Community") });
            }

            _logger.LogError($"Failed to create community. Status: {response.StatusCode}, Content: {responseContent}");
            ModelState.AddModelError("", $"Failed to create community: {responseContent}");
            await LoadPrivacyOptions(model);
            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating community");
            ModelState.AddModelError("", "An error occurred while creating the community");
            await LoadPrivacyOptions(model);
            return View(model);
        }
    }

    // POST: Community/Join/5 - Only for non-members
    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Join(int id)
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            if (!await CanUserJoinCommunity(id, currentUserId))
            {
                TempData["Error"] = "You are not eligible to join this community or are already a member.";
                return RedirectToAction(nameof(Details), new { id = id });
            }

            var jwtToken = User.FindFirst("JWT")?.Value;
            if (string.IsNullOrEmpty(jwtToken))
            {
                return RedirectToAction("Login", "Auth");
            }

            var client = _clientFactory.CreateClient("API");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwtToken);

            var response = await client.PostAsync($"api/community/{id}/join", null);

            if (response.IsSuccessStatusCode)
            {
                TempData["Success"] = "Successfully joined the community!";
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                TempData["Error"] = $"Failed to join community: {errorContent}";
            }

            return RedirectToAction(nameof(Details), new { id = id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error joining community {CommunityId}", id);
            TempData["Error"] = "An error occurred while joining the community";
            return RedirectToAction(nameof(Details), new { id = id });
        }
    }

    // POST: Community/Leave/5 - Only for members (not admins)
    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Leave(int id)
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            if (!await CanUserLeaveCommunity(id, currentUserId))
            {
                TempData["Error"] = "You cannot leave this community. Administrators must transfer admin rights before leaving.";
                return RedirectToAction(nameof(Details), new { id = id });
            }

            var jwtToken = User.FindFirst("JWT")?.Value;
            if (string.IsNullOrEmpty(jwtToken))
            {
                return RedirectToAction("Login", "Auth");
            }

            var client = _clientFactory.CreateClient("API");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwtToken);

            var response = await client.PostAsync($"api/community/{id}/leave", null);

            if (response.IsSuccessStatusCode)
            {
                TempData["Success"] = "Successfully left the community!";
                return RedirectToAction(nameof(Index));
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                TempData["Error"] = $"Failed to leave community: {errorContent}";
            }

            return RedirectToAction(nameof(Details), new { id = id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error leaving community {CommunityId}", id);
            TempData["Error"] = "An error occurred while leaving the community";
            return RedirectToAction(nameof(Details), new { id = id });
        }
    }

    // POST: Community/TransferAdmin/5 - Only for admins
    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TransferAdmin(int id, int newAdminId)
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            if (!await IsUserAdminOfCommunity(id, currentUserId))
            {
                TempData["Error"] = "You do not have permission to transfer admin rights for this community.";
                return RedirectToAction(nameof(Details), new { id = id });
            }

            var jwtToken = User.FindFirst("JWT")?.Value;
            if (string.IsNullOrEmpty(jwtToken))
            {
                return RedirectToAction("Login", "Auth");
            }

            var client = _clientFactory.CreateClient("API");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwtToken);

            var response = await client.PostAsync($"api/community/{id}/transfer-admin?newAdminId={newAdminId}", null);

            if (response.IsSuccessStatusCode)
            {
                TempData["Success"] = "Admin rights transferred successfully!";
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                TempData["Error"] = $"Failed to transfer admin: {errorContent}";
            }

            return RedirectToAction(nameof(Details), new { id = id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error transferring admin for community {CommunityId}", id);
            TempData["Error"] = "An error occurred while transferring admin rights";
            return RedirectToAction(nameof(Details), new { id = id });
        }
    }

    // POST: Community/RemoveMember/5 - Only for admins
    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveMember(int communityId, int userId)
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            
            // Check if current user is admin of the community
            if (!await IsUserAdminOfCommunity(communityId, currentUserId))
            {
                TempData["Error"] = "You do not have permission to remove members from this community.";
                return RedirectToAction(nameof(Details), new { id = communityId });
            }

            // Prevent admin from removing themselves
            if (currentUserId == userId)
            {
                TempData["Error"] = "You cannot remove yourself as an admin. Transfer admin rights first.";
                return RedirectToAction(nameof(Details), new { id = communityId });
            }

            var jwtToken = User.FindFirst("JWT")?.Value;
            if (string.IsNullOrEmpty(jwtToken))
            {
                return RedirectToAction("Login", "Auth");
            }

            var client = _clientFactory.CreateClient("API");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwtToken);

            var response = await client.DeleteAsync($"api/community/{communityId}/members/{userId}");

            if (response.IsSuccessStatusCode)
            {
                TempData["Success"] = "Member removed successfully!";
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                TempData["Error"] = $"Failed to remove member: {errorContent}";
            }

            return RedirectToAction(nameof(Details), new { id = communityId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing member from community {CommunityId}", communityId);
            TempData["Error"] = "An error occurred while removing the member";
            return RedirectToAction(nameof(Details), new { id = communityId });
        }
    }

    // GET: Community/CreatePost/5 - Only for members and admins
    [Authorize]
    public async Task<IActionResult> CreatePost(int communityId)
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            if (!await IsUserMemberOfCommunity(communityId, currentUserId))
            {
                TempData["Error"] = "You must be a member of this community to create posts.";
                return RedirectToAction(nameof(Details), new { id = communityId });
            }

            // Get community details for the view
            var client = _clientFactory.CreateClient("API");
            var token = HttpContext.Request.Cookies["auth_token"];
            
            if (!string.IsNullOrEmpty(token))
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            var communityResponse = await client.GetAsync($"api/community/{communityId}");
            if (!communityResponse.IsSuccessStatusCode)
            {
                return NotFound();
            }

            var communityContent = await communityResponse.Content.ReadAsStringAsync();
            var community = JsonSerializer.Deserialize<CommunityDetailsViewModel>(communityContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            ViewBag.Community = community;
            return View(new CreatePostViewModel { CommunityID = communityId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading create post form for community {CommunityId}", communityId);
            return View("Error");
        }
    }

    // GET: Community/Members/5 (AJAX)
    [HttpGet]
    public async Task<IActionResult> GetMembers(int communityId)
    {
        try
        {
            var client = _clientFactory.CreateClient("API");
            var token = HttpContext.Request.Cookies["auth_token"];
            
            if (!string.IsNullOrEmpty(token))
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            var response = await client.GetAsync($"api/community/{communityId}/members");
            
            if (response.IsSuccessStatusCode)
            {
                var members = await response.Content.ReadFromJsonAsync<List<CommunityMemberViewModel>>(new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                return Json(members);
            }
            
            _logger.LogError($"Failed to fetch community members for community {communityId}. Status Code: {response.StatusCode}");
            return Json(new List<CommunityMemberViewModel>());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching community members for community {CommunityId}", communityId);
            return Json(new List<CommunityMemberViewModel>());
        }
    }

    // GET: Community/Posts/5 (AJAX)
    [HttpGet]
    public async Task<IActionResult> GetCommunityPosts(int communityId)
    {
        try
        {
            var client = _clientFactory.CreateClient("API");
            var token = HttpContext.Request.Cookies["auth_token"];

            if (!string.IsNullOrEmpty(token))
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            var response = await client.GetAsync($"api/community/{communityId}/posts");

            if (response.IsSuccessStatusCode)
            {
                var posts = await response.Content.ReadFromJsonAsync<List<PostViewModel>>(new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                return Json(posts);
            }

            _logger.LogError($"Failed to fetch community posts for community {communityId}. Status Code: {response.StatusCode}");
            return Json(new List<PostViewModel>());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching community posts for community {CommunityId}", communityId);
            return Json(new List<PostViewModel>());
        }
    }

    // GET: Community/Posts/5/Partial
    [HttpGet]
    public async Task<IActionResult> GetCommunityPostsPartial(int communityId)
    {
        try
        {
            var client = _clientFactory.CreateClient("API");
            var token = HttpContext.Request.Cookies["auth_token"];
            
            if (!string.IsNullOrEmpty(token))
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            var response = await client.GetAsync($"api/community/{communityId}/posts");
            
            if (response.IsSuccessStatusCode)
            {
                var posts = await response.Content.ReadFromJsonAsync<List<PostViewModel>>(new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                return PartialView("_CommunityPosts", posts);
            }
            
            _logger.LogError($"Failed to fetch community posts for community {communityId}. Status Code: {response.StatusCode}");
            return PartialView("_CommunityPosts", new List<PostViewModel>());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching community posts for community {CommunityId}", communityId);
            return PartialView("_CommunityPosts", new List<PostViewModel>());
        }
    }


    private int? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (int.TryParse(userIdClaim, out int userId))
        {
            return userId;
        }
        return null;
    }

    private UserCommunityRole GetUserRoleInCommunity(int? userId, CommunityDetailsViewModel community, List<CommunityMemberViewModel> members)
    {
        if (!userId.HasValue || !User.Identity.IsAuthenticated)
        {
            return UserCommunityRole.NonMember;
        }

        // Check if user is the admin/creator
        if (community.IsCurrentUserAdmin )
        {
            return UserCommunityRole.Admin;
        }

        // Check if user is a member
        if (members.Any(m => m.UserID == userId.Value))
        {
            return UserCommunityRole.Member;
        }

        return UserCommunityRole.NonMember;
    }

    private async Task<bool> IsUserAdminOfCommunity(int communityId, int? userId)
    {
        if (!userId.HasValue) return false;

        try
        {
            var client = _clientFactory.CreateClient("API");
            var token = HttpContext.Request.Cookies["auth_token"];
            
            if (!string.IsNullOrEmpty(token))
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            var response = await client.GetAsync($"api/community/{communityId}");
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var community = JsonSerializer.Deserialize<CommunityDetailsViewModel>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                
                return community?.IsCurrentUserAdmin == true;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking admin status for user {UserId} in community {CommunityId}", userId, communityId);
        }

        return false;
    }

    private async Task<bool> IsUserMemberOfCommunity(int communityId, int? userId)
    {
        if (!userId.HasValue) return false;

        try
        {
            var client = _clientFactory.CreateClient("API");
            var token = HttpContext.Request.Cookies["auth_token"];
            
            if (!string.IsNullOrEmpty(token))
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            // Check if user is admin first
            if (await IsUserAdminOfCommunity(communityId, userId))
            {
                return true;
            }

            // Check if user is a member
            var response = await client.GetAsync($"api/community/{communityId}/members");
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var members = JsonSerializer.Deserialize<List<CommunityMemberViewModel>>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                
                return members?.Any(m => m.UserID == userId.Value) == true;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking membership status for user {UserId} in community {CommunityId}", userId, communityId);
        }

        return false;
    }

    private async Task<bool> CanUserJoinCommunity(int communityId, int? userId)
    {
        if (!userId.HasValue) return false;
        return !await IsUserMemberOfCommunity(communityId, userId);
    }

    private async Task<bool> CanUserLeaveCommunity(int communityId, int? userId)
    {
        if (!userId.HasValue) return false;

        // Members can leave, but admins cannot leave without transferring admin rights first
        return await IsUserMemberOfCommunity(communityId, userId) && 
               !await IsUserAdminOfCommunity(communityId, userId);
    }

    private async Task LoadPrivacyOptions(CreateCommunityViewModel model)
    {
        try
        {
            var client = _clientFactory.CreateClient("API");
            
            var privacyResponse = await client.GetAsync("api/community/privacy-options");
            if (privacyResponse.IsSuccessStatusCode)
            {
                var privacyContent = await privacyResponse.Content.ReadAsStringAsync();
                var privacyOptions = JsonSerializer.Deserialize<List<PrivacyViewModel>>(privacyContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                model.PrivacyOptions = new SelectList(privacyOptions, "PrivacyID", "Name");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading privacy options");
        }
    }

    private async Task<SelectList> GetPrivacyOptionsAsync()
    {
        try
        {
            var client = _clientFactory.CreateClient("API");
            var response = await client.GetAsync("api/community/privacy-options");
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var privacyOptions = JsonSerializer.Deserialize<List<PrivacyViewModel>>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                return new SelectList(privacyOptions, "PrivacyID", "Name");
            }
            return new SelectList(new List<PrivacyViewModel>());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting privacy options");
            return new SelectList(new List<PrivacyViewModel>());
        }
    }

    
}

// Enum to define user roles in community
public enum UserCommunityRole
{
    NonMember,
    Member,
    Admin
}