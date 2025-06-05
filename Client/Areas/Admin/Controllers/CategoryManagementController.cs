using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Client.Areas.Admin.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace Client.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    [Route("Admin/[controller]/[action]")]
    public class CategoryManagementController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CategoryManagementController(IHttpClientFactory httpClientFactory, IHttpContextAccessor httpContextAccessor)
        {
            _httpClientFactory = httpClientFactory;
            _httpContextAccessor = httpContextAccessor;
        }

        private string GetJwtToken()
        {
            return _httpContextAccessor.HttpContext.User.FindFirst("JWT")?.Value;
        }

        private HttpClient GetAuthenticatedClient()
        {
            var client = _httpClientFactory.CreateClient("API");
            var token = GetJwtToken();
            if (!string.IsNullOrEmpty(token))
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
            return client;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                var client = GetAuthenticatedClient();
                var response = await client.GetAsync("api/admin/categories");
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var categories = JsonSerializer.Deserialize<List<CategoryViewModel>>(content, 
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    
                    return View(categories);
                }

                ModelState.AddModelError("", "Error retrieving categories");
                return View(new List<CategoryViewModel>());
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"An error occurred: {ex.Message}");
                return View(new List<CategoryViewModel>());
            }
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View(new CategoryViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CategoryViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            try
            {
                var client = GetAuthenticatedClient();
                var content = new StringContent(
                    JsonSerializer.Serialize(new { 
                        CategoryName = model.CategoryName, 
                        Description = model.Description 
                    }), 
                    Encoding.UTF8, 
                    "application/json");

                var response = await client.PostAsync("api/admin/categories", content);

                if (response.IsSuccessStatusCode)
                {
                    TempData["SuccessMessage"] = "Category created successfully";
                    return RedirectToAction(nameof(Index));
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                ModelState.AddModelError("", $"Error creating category: {errorContent}");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"An error occurred: {ex.Message}");
            }

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                var client = GetAuthenticatedClient();
                var response = await client.GetAsync($"api/admin/categories/{id}");
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var category = JsonSerializer.Deserialize<CategoryViewModel>(content, 
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    
                    return View(category);
                }

                return NotFound();
            }
            catch (Exception)
            {
                return NotFound();
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, CategoryViewModel model)
        {
            if (id != model.CategoryID)
                return NotFound();

            if (!ModelState.IsValid)
                return View(model);

            try
            {
                var client = GetAuthenticatedClient();
                var content = new StringContent(
                    JsonSerializer.Serialize(new { 
                        CategoryName = model.CategoryName, 
                        Description = model.Description 
                    }), 
                    Encoding.UTF8, 
                    "application/json");

                var response = await client.PutAsync($"api/admin/categories/{id}", content);

                if (response.IsSuccessStatusCode)
                {
                    TempData["SuccessMessage"] = "Category updated successfully";
                    return RedirectToAction(nameof(Index));
                }

                
                var errorContent = await response.Content.ReadAsStringAsync();
                ModelState.AddModelError("", $"Error updating category: {errorContent}");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"An error occurred: {ex.Message}");
            }

            return View(model);
        }

        // Status toggle removed as Category model doesn't support it
    }
}
