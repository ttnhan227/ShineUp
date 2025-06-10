using Client.Areas.Admin.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Text;

namespace Client.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
[Route("Admin/[controller]/[action]")]
public class PostManagementController : Controller
{
    private readonly string _apiBaseUrl = "api/admin/PostManagement";
    private readonly IHttpClientFactory _httpClientFactory;

    public PostManagementController(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    // GET: Admin/PostManagement/Index
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var token = User.FindFirst("JWT")?.Value;
        if (string.IsNullOrEmpty(token))
        {
            return Unauthorized();
        }

        try
        {
            var client = GetAuthenticatedClient(token);
            var response = await client.GetAsync(_apiBaseUrl);

            if (!response.IsSuccessStatusCode)
            {
                return View(new List<PostViewModel>());
            }

            var json = await response.Content.ReadAsStringAsync();
            var posts = JsonConvert.DeserializeObject<List<PostViewModel>>(json);
            return View(posts);
        }
        catch (Exception ex)
        {
            // Log error
            ModelState.AddModelError("", "An error occurred while retrieving posts.");
            return View(new List<PostViewModel>());
        }
    }

    // GET: Admin/PostManagement/Details/5
    [HttpGet("{id}")]
    public async Task<IActionResult> Details(int id)
    {
        var token = User.FindFirst("JWT")?.Value;
        if (string.IsNullOrEmpty(token))
        {
            return Unauthorized();
        }

        try
        {
            var client = GetAuthenticatedClient(token);
            var response = await client.GetAsync($"{_apiBaseUrl}/{id}");

            if (!response.IsSuccessStatusCode)
            {
                return NotFound();
            }

            var json = await response.Content.ReadAsStringAsync();
            var post = JsonConvert.DeserializeObject<PostDetailViewModel>(json);
            return View(post);
        }
        catch (Exception ex)
        {
            // Log error
            ModelState.AddModelError("", "An error occurred while retrieving the post details.");
            return RedirectToAction(nameof(Index));
        }
    }

    // POST: Admin/PostManagement/UpdateStatus/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(int id, [FromForm] bool isActive)
    {
        var token = User.FindFirst("JWT")?.Value;
        if (string.IsNullOrEmpty(token))
        {
            return Unauthorized();
        }

        try
        {
            var client = GetAuthenticatedClient(token);
            var updateModel = new UpdatePostStatusViewModel
            {
                PostID = id,
                IsActive = isActive
            };

            var content = new StringContent(JsonConvert.SerializeObject(updateModel), Encoding.UTF8,
                "application/json");
            var response = await client.PutAsync($"{_apiBaseUrl}/status/{id}", content);

            if (!response.IsSuccessStatusCode)
            {
                ModelState.AddModelError("", "Failed to update post status.");
                return RedirectToAction(nameof(Index));
            }

            TempData["SuccessMessage"] = "Post status updated successfully.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            // Log error
            ModelState.AddModelError("", "An error occurred while updating the post status.");
            return RedirectToAction(nameof(Index));
        }
    }

    // POST: Admin/PostManagement/Delete/5
    [HttpPost] [ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var token = User.FindFirst("JWT")?.Value;
        if (string.IsNullOrEmpty(token))
        {
            return Unauthorized();
        }

        try
        {
            var client = GetAuthenticatedClient(token);
            var response = await client.DeleteAsync($"{_apiBaseUrl}/{id}");

            if (!response.IsSuccessStatusCode)
            {
                ModelState.AddModelError("", "Failed to delete the post.");
                return RedirectToAction(nameof(Index));
            }

            TempData["SuccessMessage"] = "Post deleted successfully.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            // Log error
            ModelState.AddModelError("", "An error occurred while deleting the post.");
            return RedirectToAction(nameof(Index));
        }
    }

    private HttpClient GetAuthenticatedClient(string token)
    {
        var client = _httpClientFactory.CreateClient("API");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }
}