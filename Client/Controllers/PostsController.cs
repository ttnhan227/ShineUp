using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Client.Models;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Rendering;

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
            var response = await client.GetAsync("api/posts");
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var posts = JsonSerializer.Deserialize<List<PostListViewModel>>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                return View(posts);
            }
            
            return View("Error");
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
            var client = _clientFactory.CreateClient("API");
            var response = await client.GetAsync($"api/posts/{id}");
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var post = JsonSerializer.Deserialize<PostViewModel>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                return View(post);
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

            return View();
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
    public async Task<IActionResult> Create(CreatePostViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await LoadDropdowns();
            return View(model);
        }

        try
        {
            var client = _clientFactory.CreateClient("API");
            
            // Get the JWT token from claims
            var token = User.FindFirst("JWT")?.Value;
            if (string.IsNullOrEmpty(token))
            {
                ModelState.AddModelError("", "Authentication token is missing");
                await LoadDropdowns();
                return View(model);
            }

            // Add the authorization header
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            
            // Create multipart form data
            var formData = new MultipartFormDataContent();
            formData.Add(new StringContent(model.Title), "Title");
            formData.Add(new StringContent(model.Content), "Content");
            
            if (model.CategoryID.HasValue)
                formData.Add(new StringContent(model.CategoryID.Value.ToString()), "CategoryID");
            if (model.PrivacyID.HasValue)
                formData.Add(new StringContent(model.PrivacyID.Value.ToString()), "PrivacyID");

            // Handle image upload if present
            if (model.Image != null)
            {
                var imageContent = new StreamContent(model.Image.OpenReadStream());
                imageContent.Headers.ContentType = new MediaTypeHeaderValue(model.Image.ContentType);
                formData.Add(imageContent, "Image", model.Image.FileName);
            }

            _logger.LogInformation($"Sending POST request to api/posts with Title: {model.Title}, Content: {model.Content}, CategoryID: {model.CategoryID}, PrivacyID: {model.PrivacyID}");
            
            var response = await client.PostAsync("api/posts", formData);
            var responseContent = await response.Content.ReadAsStringAsync();
            
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Post created successfully");
                return RedirectToAction(nameof(Index));
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
                var post = JsonSerializer.Deserialize<PostViewModel>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                var editModel = new EditPostViewModel
                {
                    PostID = post.PostID,
                    Title = post.Title,
                    Content = post.Content,
                    CurrentImageURL = post.ImageURL,
                    CategoryID = post.CategoryID,
                    PrivacyID = post.PrivacyID
                };

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
    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, EditPostViewModel model)
    {
        if (id != model.PostID)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            await LoadDropdowns();
            return View(model);
        }

        try
        {
            var client = _clientFactory.CreateClient("API");
            
            // Get the JWT token from claims
            var token = User.FindFirst("JWT")?.Value;
            if (string.IsNullOrEmpty(token))
            {
                ModelState.AddModelError("", "Authentication token is missing");
                await LoadDropdowns();
                return View(model);
            }

            // Add the authorization header
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            
            // Handle image upload if present
            if (model.Image != null)
            {
                var formData = new MultipartFormDataContent();
                if (model.Title != null)
                    formData.Add(new StringContent(model.Title), "Title");
                if (model.Content != null)
                    formData.Add(new StringContent(model.Content), "Content");
                if (model.CategoryID.HasValue)
                    formData.Add(new StringContent(model.CategoryID.Value.ToString()), "CategoryID");
                if (model.PrivacyID.HasValue)
                    formData.Add(new StringContent(model.PrivacyID.Value.ToString()), "PrivacyID");

                var imageContent = new StreamContent(model.Image.OpenReadStream());
                imageContent.Headers.ContentType = new MediaTypeHeaderValue(model.Image.ContentType);
                formData.Add(imageContent, "Image", model.Image.FileName);

                var response = await client.PutAsync($"api/posts/{id}", formData);
                if (response.IsSuccessStatusCode)
                {
                    return RedirectToAction(nameof(Index));
                }
            }
            else
            {
                // No image upload
                var response = await client.PutAsJsonAsync($"api/posts/{id}", model);
                if (response.IsSuccessStatusCode)
                {
                    return RedirectToAction(nameof(Index));
                }
            }

            ModelState.AddModelError("", "Failed to update post");
            await LoadDropdowns();
            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating post {PostId}", id);
            ModelState.AddModelError("", "An error occurred while updating the post");
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
    public async Task<IActionResult> UserPosts(int userId)
    {
        try
        {
            var client = _clientFactory.CreateClient("API");
            var response = await client.GetAsync($"api/posts/user/{userId}");
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var posts = JsonSerializer.Deserialize<List<PostListViewModel>>(content, new JsonSerializerOptions
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

    // GET: Posts/Category/5
    public async Task<IActionResult> CategoryPosts(int categoryId)
    {
        try
        {
            var client = _clientFactory.CreateClient("API");
            var response = await client.GetAsync($"api/posts/category/{categoryId}");
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var posts = JsonSerializer.Deserialize<List<PostListViewModel>>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                return View("Index", posts);
            }
            
            return View("Error");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting posts for category {CategoryId}", categoryId);
            return View("Error");
        }
    }
} 