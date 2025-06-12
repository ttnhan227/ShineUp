using Client.Areas.Admin.Models;
using Client.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using CategoryViewModel = Client.Areas.Admin.Models.CategoryViewModel;

namespace Client.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
[Route("Admin/[controller]/[action]")]
public class OpportunityManagementController : Controller
{
    private const string ApiBasePath = "api/admin/opportunities";
    private const string ApiBaseUrl = "api/admin/opportunities";
    private const string ApiBaseApplicationsUrl = "api/admin/applications";
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<OpportunityManagementController> _logger;

    public OpportunityManagementController(
        IHttpClientFactory httpClientFactory,
        IHttpContextAccessor httpContextAccessor,
        ILogger<OpportunityManagementController> logger)
    {
        _httpClientFactory = httpClientFactory;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
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

    // POST: Admin/OpportunityManagement/DeleteConfirmed/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        try
        {
            var client = GetAuthenticatedClient();
            var response = await client.DeleteAsync($"{ApiBaseUrl}/{id}");

            if (!response.IsSuccessStatusCode)
            {
                return StatusCode((int)response.StatusCode);
            }

            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting opportunity with ID {OpportunityId}", id);
            return StatusCode(500, "An error occurred while deleting the opportunity.");
        }
    }

    // POST: Admin/OpportunityManagement/UpdateStatus/5
    [HttpPost("UpdateStatus")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(int id, string status)
    {
        try
        {
            var client = GetAuthenticatedClient();
            
            // Send the status as a simple string in the request body
            var content = new StringContent(
                $"\"{status}\"",
                Encoding.UTF8,
                "application/json");
                
            var response = await client.PutAsync($"{ApiBaseUrl}/{id}/status", content);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to update opportunity status. Status: {StatusCode}, Response: {Response}", 
                    response.StatusCode, errorContent);
                    
                TempData["ErrorMessage"] = "Failed to update opportunity status. " + errorContent;
                return RedirectToAction("Details", new { id });
            }

            TempData["SuccessMessage"] = $"Opportunity has been {status.ToLower()} successfully.";
            return RedirectToAction("Details", new { id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating status for opportunity ID {OpportunityId}", id);
            TempData["ErrorMessage"] = "An error occurred while updating the opportunity status: " + ex.Message;
            return RedirectToAction("Details", new { id });
        }
    }

    // GET: Admin/OpportunityManagement/Applications/5
    [HttpGet("{id}/applications")]
    public async Task<IActionResult> Applications(int id)
    {
        try
        {
            var client = GetAuthenticatedClient();
            
            // Get applications for the opportunity
            var response = await client.GetAsync($"{ApiBasePath}/{id}/applications");
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to fetch applications. Status: {StatusCode}", response.StatusCode);
                return View("Error");
            }

            var json = await response.Content.ReadAsStringAsync();
            var applications = JsonSerializer.Deserialize<List<ApplicationListViewModel>>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            // Get opportunity details
            var opportunityResponse = await client.GetAsync($"{ApiBasePath}/{id}");
            if (!opportunityResponse.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to fetch opportunity details. Status: {StatusCode}", opportunityResponse.StatusCode);
                return View("Error");
            }

            var opportunityJson = await opportunityResponse.Content.ReadAsStringAsync();
            var opportunity = JsonSerializer.Deserialize<OpportunityDetailViewModel>(opportunityJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            // Create the view model
            var viewModel = new OpportunityApplicationsViewModel
            {
                OpportunityId = id,
                OpportunityTitle = opportunity?.Title ?? "Opportunity",
                Applications = applications?.Select(a => new ApplicationListItemViewModel
                {
                    ApplicationId = a.ApplicationId,
                    OpportunityId = a.OpportunityId,
                    OpportunityTitle = a.OpportunityTitle,
                    ApplicantName = a.UserName,
                    Email = a.UserEmail,
                    Status = a.Status,
                    AppliedAt = a.AppliedAt,
                    ReviewedAt = a.ReviewedAt
                }).ToList() ?? new(),
                StatusCounts = applications?.GroupBy(a => a.Status)
                    .ToDictionary(g => g.Key, g => g.Count()) ?? new(),
                ApplicationStatuses = applications?.Select(a => a.Status).Distinct().ToList() ?? new()
            };

            return View(viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving applications for opportunity {OpportunityId}", id);
            ModelState.AddModelError("", "An error occurred while retrieving the applications.");
            return View(new OpportunityApplicationsViewModel { OpportunityId = id, OpportunityTitle = "Opportunity" });
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

            // Set available status options from the ApplicationStatus enum
            application.AvailableStatuses = Enum.GetNames(typeof(ApplicationStatus))
                .Where(s => s != nameof(ApplicationStatus.Withdrawn)) // Exclude Withdrawn status from admin options
                .ToList();
            
            return View(application);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading application details for ID {ApplicationId}", id);
            TempData["ErrorMessage"] = "An error occurred while loading the application details.";
            return RedirectToAction("Applications", new { id = Request.RouteValues["opportunityId"] });
        }
    }
    
    // POST: Admin/OpportunityManagement/UpdateApplicationStatus/5
    [HttpPost("UpdateApplicationStatus")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateApplicationStatus(int id, string status, string reviewNotes, int opportunityId)
    {
        try
        {
            var client = GetAuthenticatedClient();
            
            // Create the request body
            var requestBody = new 
            {
                Status = status,
                ReviewNotes = reviewNotes
            };
            
            var content = new StringContent(
                JsonSerializer.Serialize(requestBody),
                Encoding.UTF8,
                "application/json");
                
            var response = await client.PutAsync($"{ApiBaseUrl}/applications/{id}/status", content);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to update application status. Status: {StatusCode}, Response: {Response}", 
                    response.StatusCode, errorContent);
                    
                TempData["ErrorMessage"] = "Failed to update application status. " + errorContent;
                return RedirectToAction("ApplicationDetails", new { id });
            }

            TempData["SuccessMessage"] = "Application status updated successfully.";
            return RedirectToAction("ApplicationDetails", new { id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating status for application ID {ApplicationId}", id);
            TempData["ErrorMessage"] = "An error occurred while updating the application status: " + ex.Message;
            return RedirectToAction("ApplicationDetails", new { id });
        }
    }

    // This is a duplicate method and should be removed
    // Keeping it for now to avoid breaking any references
    [HttpPost("applications/{id}/status")]
    [ValidateAntiForgeryToken]
    [Obsolete("Use the other UpdateApplicationStatus method instead")]
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
            model.OpportunityTypes = Enum.GetNames(typeof(OpportunityType)).ToList();
            model.OpportunityStatuses = Enum.GetNames(typeof(OpportunityStatus)).ToList();
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