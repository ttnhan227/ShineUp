using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Client.Models;
using System.Text.Json.Serialization;

namespace Client.Controllers;

[Authorize]
[Route("Posts")]
public class PostsController : Controller
{
    private readonly IHttpClientFactory _clientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<PostsController> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public PostsController(
        IHttpClientFactory clientFactory,
        IConfiguration configuration,
        ILogger<PostsController> logger)
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
        var client = _clientFactory.CreateClient("API");
        
        if (!User.Identity.IsAuthenticated)
        {
            _logger.LogWarning("User is not authenticated");
            return null;
        }
        
        var token = User.FindFirst("JWT")?.Value;
        if (string.IsNullOrEmpty(token))
        {
            _logger.LogWarning("JWT token not found in claims");
            return null;
        }

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    // POST: Posts/Create
    [Authorize]
    [HttpPost("create")]
    [Route("Posts/CreatePost")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreatePost(CreatePostViewModel model, IFormFileCollection Images, IFormFileCollection Videos, int? CommunityID)
    {
        _logger.LogInformation("Starting post creation process...");
        
        if (!ModelState.IsValid)
        {
            _logger.LogWarning("Model state is invalid. Errors: {Errors}", 
                string.Join("; ", ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)));
                    
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { 
                    success = false, 
                    message = "Validation failed",
                    errors = ModelState.ToDictionary(
                        kvp => kvp.Key,
                        kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray()
                    )
                });
            }
            
            // Reload the view with validation errors for non-AJAX requests
            return await Index();
        }

        try
        {
            _logger.LogInformation("Getting authenticated client...");
            var client = await GetAuthenticatedClient();
            
            if (client == null)
            {
                _logger.LogWarning("Authentication failed - no valid client returned");
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { 
                        success = false, 
                        message = "Authentication required",
                        redirect = Url.Action("Login", "Auth")
                    });
                }
                return RedirectToAction("Login", "Auth");
            }

            // Log form data for debugging
            _logger.LogInformation("Creating form data with Title: {Title}, CategoryID: {CategoryID}, PrivacyID: {PrivacyID}, CommunityID: {CommunityID}",
                model.Title, model.CategoryID, model.PrivacyID, CommunityID);
                
            _logger.LogInformation("Processing {ImageCount} images and {VideoCount} videos", 
                Images?.Count ?? 0, Videos?.Count ?? 0);

            // Create form data
            using var formData = new MultipartFormDataContent();
            
            // Add post data
            formData.Add(new StringContent(model.Title ?? ""), "Title");
            formData.Add(new StringContent(model.Content ?? ""), "Content");
            
            if (model.CategoryID.HasValue)
            {
                formData.Add(new StringContent(model.CategoryID.Value.ToString()), "CategoryID");
            }
            
            if (model.PrivacyID.HasValue)
            {
                formData.Add(new StringContent(model.PrivacyID.Value.ToString()), "PrivacyID");
            }
            
            if (CommunityID.HasValue)
            {
                formData.Add(new StringContent(CommunityID.Value.ToString()), "CommunityID");
                _logger.LogInformation("Added CommunityID: {CommunityID} to form data", CommunityID.Value);
            }

            // Add images
            if (Images != null && Images.Count > 0)
            {
                foreach (var image in Images)
                {
                    if (image != null && image.Length > 0)
                    {
                        _logger.LogInformation("Processing image: {FileName} ({Length} bytes)", 
                            image.FileName, image.Length);
                            
                        using var ms = new MemoryStream();
                        await image.CopyToAsync(ms);
                        var fileContent = new ByteArrayContent(ms.ToArray());
                        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse(image.ContentType);
                        formData.Add(fileContent, "ImageFiles", image.FileName);
                    }
                }
            }

            // Add videos
            if (Videos != null && Videos.Count > 0)
            {
                foreach (var video in Videos)
                {
                    if (video != null && video.Length > 0)
                    {
                        _logger.LogInformation("Processing video: {FileName} ({Length} bytes)", 
                            video.FileName, video.Length);
                            
                        using var ms = new MemoryStream();
                        await video.CopyToAsync(ms);
                        var fileContent = new ByteArrayContent(ms.ToArray());
                        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse(video.ContentType);
                        formData.Add(fileContent, "VideoFiles", video.FileName);
                    }
                }
            }

            _logger.LogInformation("Sending request to API...");
            var response = await client.PostAsync("api/posts", formData);
            var responseContent = await response.Content.ReadAsStringAsync();
            
            _logger.LogInformation("API response: {StatusCode} - {Content}", 
                response.StatusCode, responseContent);
            
            if (response.IsSuccessStatusCode)
            {
                var successMessage = "Post created successfully!";
                _logger.LogInformation(successMessage);
                
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { 
                        success = true, 
                        message = successMessage,
                        redirect = Url.Action("Index", "Posts")
                    });
                }
                
                TempData["SuccessMessage"] = successMessage;
                return RedirectToAction(nameof(Index));
            }
            else
            {
                string errorMessage;
                try 
                {
                    var errorResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);
                    errorMessage = errorResponse.GetProperty("title").GetString() ?? "Error creating post";
                    
                    if (errorResponse.TryGetProperty("errors", out var errors))
                    {
                        var errorList = new List<string>();
                        foreach (var error in errors.EnumerateObject())
                        {
                            errorList.AddRange(error.Value.EnumerateArray().Select(e => e.GetString()));
                        }
                        errorMessage = string.Join(" ", errorList);
                    }
                }
                catch
                {
                    errorMessage = responseContent ?? "An unknown error occurred";
                }
                
                _logger.LogError("Error creating post: {StatusCode} - {ErrorMessage}", 
                    response.StatusCode, errorMessage);
                    
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { 
                        success = false, 
                        message = errorMessage,
                        statusCode = response.StatusCode
                    });
                }
                
                ModelState.AddModelError(string.Empty, errorMessage);
                return await Index();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error creating post");
            var errorMessage = "An unexpected error occurred while creating the post.";
            
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { 
                    success = false, 
                    message = errorMessage,
                    error = ex.Message
                });
            }
            
            ModelState.AddModelError(string.Empty, errorMessage);
            return await Index();
        }
    }

    // GET: Posts
    [HttpGet("")]
    [HttpGet("index")]
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

            // Get posts
            var response = await client.GetAsync("api/posts");
            if (!response.IsSuccessStatusCode)
            {
                return View("Error");
            }

        var content = await response.Content.ReadAsStringAsync();
        var posts = JsonSerializer.Deserialize<List<PostViewModel>>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        // Get user profile images and like status for posts
        foreach (var post in posts)
        {
            // Get user profile image
            var userResponse = await client.GetAsync($"api/UserProfile/username/{post.Username}");
            if (userResponse.IsSuccessStatusCode)
            {
                var userContent = await userResponse.Content.ReadAsStringAsync();
                var user = JsonSerializer.Deserialize<UserViewModel>(userContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                post.ProfileImageURL = user?.ProfileImageURL;
            }

            // Check if current user has liked this post
            if (!string.IsNullOrEmpty(token))
            {
                var hasLikedResponse = await client.GetAsync($"api/social/posts/{post.PostID}/has-liked");
                if (hasLikedResponse.IsSuccessStatusCode)
                {
                    var hasLikedContent = await hasLikedResponse.Content.ReadAsStringAsync();
                    post.HasLiked = JsonSerializer.Deserialize<bool>(hasLikedContent);
                }
            }
            else
            {
                post.HasLiked = false;
            }
        }

        // Get categories
        var categoriesResponse = await client.GetAsync("api/categories");
        if (categoriesResponse.IsSuccessStatusCode)
        {
            var categoriesContent = await categoriesResponse.Content.ReadAsStringAsync();
            var categories = JsonSerializer.Deserialize<List<CategoryViewModel>>(categoriesContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            ViewBag.Categories = new SelectList(categories, "CategoryID", "CategoryName");
        }

        // Get user's communities if authenticated
        if (User.Identity?.IsAuthenticated == true)
        {
            try 
            {
                _logger.LogInformation("Fetching user communities from API...");
                
                // Use the token from the cookie or from the User claims
                var authToken = HttpContext.Request.Cookies["auth_token"] ?? 
                               User.FindFirst("JWT")?.Value;
                
                if (string.IsNullOrEmpty(authToken))
                {
                    _logger.LogWarning("No auth token found in cookies or claims");
                    ViewBag.UserCommunities = new SelectList(new List<object>());
                }
                else
                {
                    // Create a new client with the token
                    var authClient = _clientFactory.CreateClient("API");
                    authClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authToken);
                    
                    var communityResponse = await authClient.GetAsync("api/community/user/memberships");
                    _logger.LogInformation($"Community API response status: {communityResponse.StatusCode}");
                    
                    if (communityResponse.IsSuccessStatusCode)
                    {
                        var communityContent = await communityResponse.Content.ReadAsStringAsync();
                        var options = new JsonSerializerOptions 
                        { 
                            PropertyNameCaseInsensitive = true,
                            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                        };
                        
                        var communities = JsonSerializer.Deserialize<List<CommunityViewModel>>(communityContent, options);
                        
                        if (communities?.Any() == true)
                        {
                            var communityList = communities
                                .Where(c => c != null)
                                .OrderBy(c => c.Name)
                                .Select(c => new 
                                {
                                    Value = c.CommunityID.ToString(),
                                    Text = c.Name
                                })
                                .ToList();
                                
                            ViewBag.UserCommunities = new SelectList(communityList, "Value", "Text");
                            _logger.LogInformation($"Successfully loaded {communityList.Count} communities");
                        }
                        else
                        {
                            _logger.LogInformation("No communities found for user");
                            ViewBag.UserCommunities = new SelectList(new List<object>());
                        }
                    }
                    else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    {
                        _logger.LogWarning("Unauthorized access to communities. Token might be invalid or expired.");
                        ViewBag.UserCommunities = new SelectList(new List<object>());
                    }
                    else
                    {
                        _logger.LogError($"Failed to fetch communities. Status: {response.StatusCode}");
                        ViewBag.UserCommunities = new SelectList(new List<object>());
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching user communities");
                ViewBag.UserCommunities = new SelectList(new List<object>());
            }
        }
        else
        {
            _logger.LogInformation("User is not authenticated, not fetching communities");
            ViewBag.UserCommunities = new SelectList(new List<object>());
        }

        // Get privacy options
        var privacyResponse = await client.GetAsync("api/privacy");
        if (privacyResponse.IsSuccessStatusCode)
        {
            var privacyContent = await privacyResponse.Content.ReadAsStringAsync();
            var privacyOptions = JsonSerializer.Deserialize<List<PrivacyViewModel>>(privacyContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            ViewBag.PrivacyOptions = new SelectList(privacyOptions, "PrivacyID", "Name");
        }

        return View(posts);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error getting posts");
        return View("Error");
    }
}

    // GET: Posts/Details/5
    [HttpGet("details/{id}")]
    public async Task<IActionResult> Details(int id)
    {
        try
        {
            try
            {
                var client = _clientFactory.CreateClient("API");
                var token = HttpContext.Request.Cookies["auth_token"];
                if (!string.IsNullOrEmpty(token))
                {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                }
                
                // Get post details
                var response = await client.GetAsync($"api/posts/{id}");
                
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Failed to get post {PostId}. Status code: {StatusCode}", id, response.StatusCode);
                    return NotFound();
                }
                
                var content = await response.Content.ReadAsStringAsync();
                var post = JsonSerializer.Deserialize<PostDetailsViewModel>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                // Get user profile image
                var userResponse = await client.GetAsync($"api/UserProfile/username/{post.Username}");
                if (userResponse.IsSuccessStatusCode)
                {
                    var userContent = await userResponse.Content.ReadAsStringAsync();
                    var user = JsonSerializer.Deserialize<UserViewModel>(userContent, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    post.ProfileImageURL = user?.ProfileImageURL;
                }

                // Get comments for this post
                var commentsResponse = await client.GetAsync($"api/social/posts/{id}/comments");
                if (commentsResponse.IsSuccessStatusCode)
                {
                    var commentsContent = await commentsResponse.Content.ReadAsStringAsync();
                    var comments = JsonSerializer.Deserialize<List<CommentViewModel>>(commentsContent, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    
                    // Get profile images for commenters
                    if (comments != null && comments.Any())
                    {
                        var userIds = comments.Select(c => c.UserID).Distinct().ToList();
                        var usersResponse = await client.PostAsJsonAsync("api/UserProfile/profiles", new { UserIds = userIds });
                        
                        if (usersResponse.IsSuccessStatusCode)
                        {
                            var usersContent = await usersResponse.Content.ReadAsStringAsync();
                            var users = JsonSerializer.Deserialize<List<UserViewModel>>(usersContent, new JsonSerializerOptions
                            {
                                PropertyNameCaseInsensitive = true
                            });
                            
                            var userDictionary = users?.ToDictionary(u => u.UserID, u => u.ProfileImageURL) ?? new Dictionary<int, string>();
                            
                            foreach (var comment in comments)
                            {
                                if (userDictionary.TryGetValue(comment.UserID, out var profileImageUrl))
                                {
                                    comment.ProfileImageURL = profileImageUrl;
                                }
                            }
                        }
                    }
                    
                    ViewBag.Comments = comments ?? new List<CommentViewModel>();
                }
                else
                {
                    ViewBag.Comments = new List<CommentViewModel>();
                }

                // Check if current user has liked this post
                if (!string.IsNullOrEmpty(token))
                {
                    var hasLikedResponse = await client.GetAsync($"api/social/posts/{id}/has-liked");
                    if (hasLikedResponse.IsSuccessStatusCode)
                    {
                        var hasLikedContent = await hasLikedResponse.Content.ReadAsStringAsync();
                        post.HasLiked = JsonSerializer.Deserialize<bool>(hasLikedContent);
                    }
                }
                else
                {
                    post.HasLiked = false;
                }

                // Get like count
                var likesResponse = await client.GetAsync($"api/social/posts/{id}/like-count");
                if (likesResponse.IsSuccessStatusCode)
                {
                    var likesCountContent = await likesResponse.Content.ReadAsStringAsync();
                    if (int.TryParse(likesCountContent, out int likesCount))
                    {
                        post.LikesCount = likesCount;
                    }
                }

                return View(post);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting post details for post {PostId}", id);
                return StatusCode(500, "An error occurred while retrieving the post details.");
            }
            
            return NotFound();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting post {PostId}", id);
            return View("Error");
        }
    }

    // GET: Posts/Create
    [Authorize]
    [HttpGet("create")]
    [Route("Posts/Create")]
    public async Task<IActionResult> Create()
    {
        try
        {
            var client = _clientFactory.CreateClient("API");
            var viewModel = new CreatePostViewModel();
            
            // Get categories
            var categoriesResponse = await client.GetAsync("api/categories");
            if (categoriesResponse.IsSuccessStatusCode)
            {
                var categoriesContent = await categoriesResponse.Content.ReadAsStringAsync();
                var categories = JsonSerializer.Deserialize<List<CategoryViewModel>>(categoriesContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                viewModel.Categories = new SelectList(categories, "CategoryID", "CategoryName");
            }

            // Get privacy options
            var privacyResponse = await client.GetAsync("api/privacy");
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
            _logger.LogError(ex, "Error loading create post form data");
            return View("Error");
        }
    }

    // POST: Posts/Create
    [Authorize]
    [HttpPost("create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([FromForm] CreatePostViewModel model)
    {
        if (!ModelState.IsValid)
        {
            _logger.LogWarning("CreatePost ModelState is invalid.");
            foreach (var state in ModelState.Values)
            {
                foreach (var error in state.Errors)
                {
                    _logger.LogError($"ModelState error: {error.ErrorMessage}");
                }
            }
            ViewBag.Categories = await GetCategoriesAsync();
            ViewBag.PrivacyOptions = await GetPrivacyOptionsAsync();
            return RedirectToAction(nameof(Index));
        }

        try
        {
            var jwtToken = User.FindFirst("JWT")?.Value;
            if (string.IsNullOrEmpty(jwtToken))
            {
                _logger.LogError("JWT token is missing");
                return RedirectToAction("Login", "Auth", new { returnUrl = Url.Action("Create", "Posts") });
            }

            var client = _clientFactory.CreateClient("API");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwtToken);

            var formData = new MultipartFormDataContent();
            formData.Add(new StringContent(model.Title), "Title");
            formData.Add(new StringContent(model.Content), "Content");
            formData.Add(new StringContent(model.CategoryID.ToString()), "CategoryID");
            formData.Add(new StringContent((model.PrivacyID ?? 1).ToString()), "PrivacyID");

            // Handle image uploads
            if (model.Images != null && model.Images.Any())
            {
                foreach (var image in model.Images)
                {
                    if (image != null && image.Length > 0)
                    {
                        var imageContent = new StreamContent(image.OpenReadStream());
                        imageContent.Headers.ContentType = new MediaTypeHeaderValue(image.ContentType);
                        formData.Add(imageContent, "MediaFiles", image.FileName);
                        formData.Add(new StringContent("image"), "MediaTypes");
                    }
                }
            }

            // Handle video uploads
            if (model.Videos != null && model.Videos.Any())
            {
                foreach (var video in model.Videos)
                {
                    if (video != null && video.Length > 0)
                    {
                        var videoContent = new StreamContent(video.OpenReadStream());
                        videoContent.Headers.ContentType = new MediaTypeHeaderValue(video.ContentType);
                        formData.Add(videoContent, "MediaFiles", video.FileName);
                        formData.Add(new StringContent("video"), "MediaTypes");
                    }
                }
            }

            _logger.LogInformation($"Sending POST request to api/posts with Title: {model.Title}, Content: {model.Content}, CategoryID: {model.CategoryID}, PrivacyID: {model.PrivacyID}");
            
            var response = await client.PostAsync("api/posts", formData);
            var responseContent = await response.Content.ReadAsStringAsync();
            
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Post created successfully");
                return RedirectToAction(nameof(Index));
            }
            
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                _logger.LogWarning("Token expired or invalid, redirecting to login");
                return RedirectToAction("Login", "Auth", new { returnUrl = Url.Action("Create", "Posts") });
            }
            
            _logger.LogError($"Failed to create post. Status: {response.StatusCode}, Content: {responseContent}");
            ModelState.AddModelError("", $"Failed to create post: {responseContent}");
            await LoadDropdowns();
            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating post");
            ModelState.AddModelError("", "An error occurred while creating the post");
            await LoadDropdowns();
            return View(model);
        }
    }

    // Helper method to load dropdowns
    private async Task LoadDropdowns()
    {
        try
        {
            var client = _clientFactory.CreateClient("API");
            
            // Get categories
            var categoriesResponse = await client.GetAsync("api/categories");
            if (categoriesResponse.IsSuccessStatusCode)
            {
                var categoriesContent = await categoriesResponse.Content.ReadAsStringAsync();
                var categories = JsonSerializer.Deserialize<List<CategoryViewModel>>(categoriesContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                ViewBag.Categories = new SelectList(categories, "CategoryID", "CategoryName");
            }

            // Get privacy options
            var privacyResponse = await client.GetAsync("api/privacy");
            if (privacyResponse.IsSuccessStatusCode)
            {
                var privacyContent = await privacyResponse.Content.ReadAsStringAsync();
                var privacyOptions = JsonSerializer.Deserialize<List<PrivacyViewModel>>(privacyContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                ViewBag.PrivacyOptions = new SelectList(privacyOptions, "PrivacyID", "Name");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading dropdown data");
        }
    }

    // GET: Posts/Edit/5
    [Authorize]
    [HttpGet("edit/{id}")]
    public async Task<IActionResult> Edit(int id)
    {
        try
        {
            var client = _clientFactory.CreateClient("API");
            var response = await client.GetAsync($"api/posts/{id}");
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var post = JsonSerializer.Deserialize<PostDetailsViewModel>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                var editModel = new EditPostViewModel
                {
                    PostID = post.PostID,
                    Title = post.Title,
                    Content = post.Content,
                    CategoryID = post.CategoryID ?? 1,
                    PrivacyID = post.PrivacyID ?? 1,
                    CurrentMediaFiles = post.MediaFiles
                };

                // Load dropdowns before showing the form
                await LoadDropdowns();
                return View(editModel);
            }
            
            return NotFound();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting post for edit {PostId}", id);
            return View("Error");
        }
    }

    // POST: Posts/Edit/5
    [HttpPost("edit/{id}")]
    [ValidateAntiForgeryToken]
    [Authorize]
    public async Task<IActionResult> Edit(int id, EditPostViewModel model, List<IFormFile> Images, List<IFormFile> Videos)
    {
        try
        {
            if (id != model.PostID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                var client = _clientFactory.CreateClient("API");
                var content = new MultipartFormDataContent();

                // Add basic post data
                content.Add(new StringContent(model.Title), "Title");
                content.Add(new StringContent(model.Content), "Content");
                content.Add(new StringContent(model.CategoryID.ToString()), "CategoryID");
                content.Add(new StringContent(model.PrivacyID.ToString()), "PrivacyID");

                // Add new images
                if (Images != null)
                {
                    foreach (var image in Images)
                    {
                        if (image.Length > 0)
                        {
                            var imageContent = new StreamContent(image.OpenReadStream());
                            content.Add(imageContent, "Images", image.FileName);
                        }
                    }
                }

                // Add new videos
                if (Videos != null)
                {
                    foreach (var video in Videos)
                    {
                        if (video.Length > 0)
                        {
                            var videoContent = new StreamContent(video.OpenReadStream());
                            content.Add(videoContent, "Videos", video.FileName);
                        }
                    }
                }

                var response = await client.PutAsync($"api/posts/{id}", content);

                if (response.IsSuccessStatusCode)
                {
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    ModelState.AddModelError("", error);
                }
            }

            // If we got this far, something failed, redisplay form
            await LoadDropdowns();
            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error editing post {PostId}", id);
            ModelState.AddModelError("", "An error occurred while editing the post.");
            await LoadDropdowns();
            return View(model);
        }
    }

    // POST: Posts/Delete/5
    [Authorize]
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        try
        {
            var client = _clientFactory.CreateClient("API");
            
            // Get the JWT token from claims
            var token = User.FindFirst("JWT")?.Value;
            if (string.IsNullOrEmpty(token))
            {
                return Unauthorized("Authentication token is missing");
            }

            // Add the authorization header
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            
            var response = await client.DeleteAsync($"api/posts/{id}");
            
            if (response.IsSuccessStatusCode)
            {
                return RedirectToAction(nameof(Index));
            }
            
            return View("Error");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting post {PostId}", id);
            return View("Error");
        }
    }

    // GET: Posts/User/5
    // This method is intended for returning a view, not for AJAX calls.
    // It conflicts with the AJAX UserPosts method below.
    /*
    public async Task<IActionResult> UserPosts(int userId)
    {
        try
        {
            var client = _clientFactory.CreateClient("API");
            var response = await client.GetAsync($"api/posts/user/{userId}");
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var posts = JsonSerializer.Deserialize<List<PostViewModel>>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                return View("Index", posts);
            }
            
            return View("Error");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting posts for user {UserId}", userId);
            return View("Error");
        }
    }
    */

    // GET: Posts/Category/5
    [HttpGet("category/{categoryId}")]
    public async Task<IActionResult> CategoryPosts(int categoryId)
    {
        try
        {
            var client = _clientFactory.CreateClient("API");
            
            // Get posts for category
            var response = await client.GetAsync($"api/posts/category/{categoryId}");
            if (!response.IsSuccessStatusCode)
            {
                return View("Error");
            }
            
            var content = await response.Content.ReadAsStringAsync();
            var posts = JsonSerializer.Deserialize<List<PostViewModel>>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            // Get user profile images for posts
            foreach (var post in posts)
            {
                var userResponse = await client.GetAsync($"api/UserProfile/{post.UserID}");
                if (userResponse.IsSuccessStatusCode)
                {
                    var userContent = await userResponse.Content.ReadAsStringAsync();
                    var user = JsonSerializer.Deserialize<UserViewModel>(userContent, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    post.ProfileImageURL = user?.ProfileImageURL;
                }
            }

            // Get categories
            var categoriesResponse = await client.GetAsync("api/categories");
            if (categoriesResponse.IsSuccessStatusCode)
            {
                var categoriesContent = await categoriesResponse.Content.ReadAsStringAsync();
                var categories = JsonSerializer.Deserialize<List<CategoryViewModel>>(categoriesContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                ViewBag.Categories = new SelectList(categories, "CategoryID", "CategoryName");
                ViewBag.SelectedCategory = categoryId;
            }

            return View("Index", posts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting posts for category {CategoryId}", categoryId);
            return View("Error");
        }
    }

    private async Task<SelectList> GetCategoriesAsync()
    {
        try
        {
            var client = _clientFactory.CreateClient("API");
            var response = await client.GetAsync("api/categories");
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var categories = JsonSerializer.Deserialize<List<CategoryViewModel>>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                return new SelectList(categories, "CategoryID", "CategoryName");
            }
            return new SelectList(new List<CategoryViewModel>());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting categories");
            return new SelectList(new List<CategoryViewModel>());
        }
    }

    private async Task<SelectList> GetPrivacyOptionsAsync()
    {
        try
        {
            var client = _clientFactory.CreateClient("API");
            var response = await client.GetAsync("api/privacy");
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

    [HttpGet("user/{userId}")]
    public async Task<IActionResult> UserPosts(int userId)
    {
        try
        {
            var token = HttpContext.Session.GetString("JWTToken");
            var client = _clientFactory.CreateClient("API");

            if (!string.IsNullOrEmpty(token))
            {
                 client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
           
            var response = await client.GetAsync($"api/posts/user/{userId}");
            
            if (response.IsSuccessStatusCode)
            {
                var posts = await response.Content.ReadFromJsonAsync<List<PostViewModel>>(new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                return Json(posts);
            }
            
             _logger.LogError($"Failed to fetch user posts for user {{UserId}}. Status Code: {{StatusCode}}", userId, response.StatusCode);
            return Json(new List<PostViewModel>());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching user posts for user {UserId}", userId);
            return Json(new List<PostViewModel>());
        }
    }

    [HttpGet("user/{userId}/partial")]
    public async Task<IActionResult> UserPostsPartial(int userId)
    {
        try
        {
            var token = HttpContext.Session.GetString("JWTToken");
            var client = _clientFactory.CreateClient("API");

            if (!string.IsNullOrEmpty(token))
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
           
            var response = await client.GetAsync($"api/posts/user/{userId}");
            
            if (response.IsSuccessStatusCode)
            {
                var posts = await response.Content.ReadFromJsonAsync<List<PostViewModel>>(new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                return PartialView("_UserPosts", posts);
            }
            
            _logger.LogError($"Failed to fetch user posts for user {{UserId}}. Status Code: {{StatusCode}}", userId, response.StatusCode);
            return PartialView("_UserPosts", new List<PostViewModel>());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching user posts for user {UserId}", userId);
            return PartialView("_UserPosts", new List<PostViewModel>());
        }
    }
} 