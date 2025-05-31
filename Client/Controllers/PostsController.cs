using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Client.Models;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Net;

namespace Client.Controllers;

public class PostsController : Controller
{
    private readonly IHttpClientFactory _clientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<PostsController> _logger;

    public PostsController(
        IHttpClientFactory clientFactory,
        IConfiguration configuration,
        ILogger<PostsController> logger)
    {
        _clientFactory = clientFactory;
        _configuration = configuration;
        _logger = logger;
    }

    // GET: Posts
    public async Task<IActionResult> Index()
    {
        try
        {
            var client = _clientFactory.CreateClient("API");
            
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
    [HttpPost]
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
    [HttpPost]
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

    [HttpGet]
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

    [HttpGet]
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