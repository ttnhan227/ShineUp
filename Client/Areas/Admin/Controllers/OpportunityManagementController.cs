using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Client.Areas.Admin.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace Client.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    [Route("Admin/[controller]/[action]")]
    public class OpportunityManagementController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private const string ApiBaseUrl = "api/admin/opportunities";

        public OpportunityManagementController(
            IHttpClientFactory httpClientFactory, 
            IHttpContextAccessor httpContextAccessor)
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

        // GET: Admin/OpportunityManagement
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            try
            {
                var client = GetAuthenticatedClient();
                var response = await client.GetAsync(ApiBaseUrl);
                
                if (!response.IsSuccessStatusCode)
                {
                    return View(new List<OpportunityListViewModel>());
                }

                var json = await response.Content.ReadAsStringAsync();
                var opportunities = JsonSerializer.Deserialize<List<OpportunityListViewModel>>(json, 
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                
                return View(opportunities);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "An error occurred while retrieving opportunities.");
                return View(new List<OpportunityListViewModel>());
            }
        }

        // GET: Admin/OpportunityManagement/Details/5
        [HttpGet("{id}")]
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var client = GetAuthenticatedClient();
                var response = await client.GetAsync($"{ApiBaseUrl}/{id}");
                
                if (!response.IsSuccessStatusCode)
                {
                    return NotFound();
                }

                var json = await response.Content.ReadAsStringAsync();
                var opportunity = JsonSerializer.Deserialize<OpportunityDetailViewModel>(json, 
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                
                return View(opportunity);
            }
            catch (Exception)
            {
                ModelState.AddModelError("", "An error occurred while retrieving the opportunity details.");
                return View();
            }
        }

        // GET: Admin/OpportunityManagement/Create
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var model = new OpportunityCreateEditViewModel();
            await PopulateDropdowns(model);
            return View(model);
        }

        // POST: Admin/OpportunityManagement/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(OpportunityCreateEditViewModel model)
        {
            if (!ModelState.IsValid)
            {
                await PopulateDropdowns(model);
                return View(model);
            }

            try
            {
                var client = GetAuthenticatedClient();
                var content = new StringContent(
                    JsonSerializer.Serialize(model), 
                    Encoding.UTF8, 
                    "application/json");
                
                var response = await client.PostAsync(ApiBaseUrl, content);
                
                if (response.IsSuccessStatusCode)
                {
                    TempData["SuccessMessage"] = "Opportunity created successfully.";
                    return RedirectToAction(nameof(Index));
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                ModelState.AddModelError("", $"Failed to create opportunity: {errorContent}");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"An error occurred while creating the opportunity: {ex.Message}");
            }

            await PopulateDropdowns(model);
            return View(model);
        }

        // GET: Admin/OpportunityManagement/Edit/5
        [HttpGet("{id}")]
        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                var client = GetAuthenticatedClient();
                var response = await client.GetAsync($"{ApiBaseUrl}/{id}");
                
                if (!response.IsSuccessStatusCode)
                {
                    return NotFound();
                }

                var json = await response.Content.ReadAsStringAsync();
                var opportunity = JsonSerializer.Deserialize<OpportunityCreateEditViewModel>(json, 
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                
                if (opportunity == null)
                {
                    return NotFound();
                }

                await PopulateDropdowns(opportunity);
                return View(opportunity);
            }
            catch (Exception)
            {
                ModelState.AddModelError("", "An error occurred while retrieving the opportunity for editing.");
                return View();
            }
        }

        // POST: Admin/OpportunityManagement/Edit/5
        [HttpPost("{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, OpportunityCreateEditViewModel model)
        {
            if (id != model.Id)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                await PopulateDropdowns(model);
                return View(model);
            }

            try
            {
                var client = GetAuthenticatedClient();
                var content = new StringContent(
                    JsonSerializer.Serialize(model), 
                    Encoding.UTF8, 
                    "application/json");
                
                var response = await client.PutAsync($"{ApiBaseUrl}/{id}", content);
                
                if (response.IsSuccessStatusCode)
                {
                    TempData["SuccessMessage"] = "Opportunity updated successfully.";
                    return RedirectToAction(nameof(Index));
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                ModelState.AddModelError("", $"Failed to update opportunity: {errorContent}");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"An error occurred while updating the opportunity: {ex.Message}");
            }

            await PopulateDropdowns(model);
            return View(model);
        }

        // GET: Admin/OpportunityManagement/Delete/5
        [HttpGet("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var client = GetAuthenticatedClient();
                var response = await client.GetAsync($"{ApiBaseUrl}/{id}");
                
                if (!response.IsSuccessStatusCode)
                {
                    return NotFound();
                }

                var json = await response.Content.ReadAsStringAsync();
                var opportunity = JsonSerializer.Deserialize<OpportunityDetailViewModel>(json, 
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                
                if (opportunity == null)
                {
                    return NotFound();
                }

                return View(opportunity);
            }
            catch (Exception)
            {
                ModelState.AddModelError("", "An error occurred while retrieving the opportunity for deletion.");
                return View();
            }
        }

        // POST: Admin/OpportunityManagement/Delete/5
        [HttpPost("{id}"), ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var client = GetAuthenticatedClient();
                var response = await client.DeleteAsync($"{ApiBaseUrl}/{id}");
                
                if (response.IsSuccessStatusCode)
                {
                    TempData["SuccessMessage"] = "Opportunity deleted successfully.";
                    return RedirectToAction(nameof(Index));
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                TempData["ErrorMessage"] = $"Failed to delete opportunity: {errorContent}";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"An error occurred while deleting the opportunity: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: Admin/OpportunityManagement/Applications/5
        [HttpGet("{id}/applications")]
        public async Task<IActionResult> Applications(int id)
        {
            try
            {
                var client = GetAuthenticatedClient();
                var response = await client.GetAsync($"{ApiBaseUrl}/{id}/applications");
                
                if (!response.IsSuccessStatusCode)
                {
                    return View(new List<ApplicationListViewModel>());
                }

                var json = await response.Content.ReadAsStringAsync();
                var applications = JsonSerializer.Deserialize<List<ApplicationListViewModel>>(json, 
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                
                ViewBag.OpportunityId = id;
                return View(applications);
            }
            catch (Exception)
            {
                ModelState.AddModelError("", "An error occurred while retrieving the applications.");
                return View(new List<ApplicationListViewModel>());
            }
        }

        // GET: Admin/OpportunityManagement/ApplicationDetails/5
        [HttpGet("applications/{id}")]
        public async Task<IActionResult> ApplicationDetails(int id)
        {
            try
            {
                var client = GetAuthenticatedClient();
                var response = await client.GetAsync($"{ApiBaseUrl}/applications/{id}");
                
                if (!response.IsSuccessStatusCode)
                {
                    return NotFound();
                }

                var json = await response.Content.ReadAsStringAsync();
                var application = JsonSerializer.Deserialize<ApplicationDetailViewModel>(json, 
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                
                if (application == null)
                {
                    return NotFound();
                }

                var model = new UpdateApplicationStatusViewModel
                {
                    ApplicationId = application.ApplicationId,
                    Status = application.Status,
                    ReviewNotes = application.ReviewNotes,
                    StatusOptions = new List<string> { "Pending", "InReview", "Approved", "Rejected" }
                };

                ViewBag.Application = application;
                return View(model);
            }
            catch (Exception)
            {
                ModelState.AddModelError("", "An error occurred while retrieving the application details.");
                return View();
            }
        }

        // POST: Admin/OpportunityManagement/UpdateApplicationStatus/5
        [HttpPost("applications/{id}/status")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateApplicationStatus(int id, UpdateApplicationStatusViewModel model)
        {
            if (id != model.ApplicationId)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                return RedirectToAction(nameof(ApplicationDetails), new { id });
            }

            try
            {
                var client = GetAuthenticatedClient();
                var content = new StringContent(
                    JsonSerializer.Serialize(model), 
                    Encoding.UTF8, 
                    "application/json");
                
                var response = await client.PutAsync($"{ApiBaseUrl}/applications/{id}/status", content);
                
                if (response.IsSuccessStatusCode)
                {
                    TempData["SuccessMessage"] = "Application status updated successfully.";
                    return RedirectToAction(nameof(ApplicationDetails), new { id });
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                TempData["ErrorMessage"] = $"Failed to update application status: {errorContent}";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"An error occurred while updating the application status: {ex.Message}";
            }

            return RedirectToAction(nameof(ApplicationDetails), new { id });
        }

        private async Task PopulateDropdowns(OpportunityCreateEditViewModel model)
        {
            try
            {
                var client = GetAuthenticatedClient();
                
                // Get categories
                var categoriesResponse = await client.GetAsync("api/admin/categories");
                if (categoriesResponse.IsSuccessStatusCode)
                {
                    var categoriesJson = await categoriesResponse.Content.ReadAsStringAsync();
                    model.Categories = JsonSerializer.Deserialize<List<CategoryViewModel>>(categoriesJson,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                }
                
                // Populate opportunity types and statuses from client enums
                model.OpportunityTypes = Enum.GetNames(typeof(Client.Models.OpportunityType)).ToList();
                model.OpportunityStatuses = Enum.GetNames(typeof(Client.Models.OpportunityStatus)).ToList();
            }
            catch (Exception)
            {
                // If we can't load the dropdowns, initialize empty lists to avoid null reference exceptions
                model.Categories ??= new List<CategoryViewModel>();
                model.OpportunityTypes ??= new List<string>();
                model.OpportunityStatuses ??= new List<string>();
            }
        }
    }
}
