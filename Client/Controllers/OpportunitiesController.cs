using Client.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Client.Controllers;

[Authorize]
[Route("Opportunities")]
public class OpportunitiesController : Controller
{
    private readonly IHttpClientFactory _clientFactory;
    private readonly IConfiguration _configuration;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly ILogger<OpportunitiesController> _logger;

    public OpportunitiesController(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<OpportunitiesController> logger)
    {
        _clientFactory = httpClientFactory;
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

    // GET: Opportunities
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> Index(int? id = null, int? categoryId = null)
    {
        // If id is provided in the route, use it as categoryId
        categoryId ??= id;
        var client = _clientFactory.CreateClient("API");
        var apiUrl = categoryId.HasValue
            ? $"api/opportunities/category/{categoryId}"
            : "api/opportunities";

        Console.WriteLine($"Fetching opportunities from: {apiUrl}");

        // Get categories for the filter dropdown
        ViewBag.Categories = await GetCategories(client);

        try
        {
            var response = await client.GetAsync(apiUrl);
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var opportunities = JsonSerializer.Deserialize<List<OpportunityViewModel>>(content, _jsonOptions);
                return View(opportunities);
            }

            return HandleError(response.StatusCode);
        }
        catch (HttpRequestException)
        {
            return View("Error", new ErrorViewModel { RequestId = HttpContext.TraceIdentifier });
        }
    }

    // GET: Opportunities/Details/5
    [HttpGet("details/{id}")]
    [AllowAnonymous]
    public async Task<IActionResult> Details(int id)
    {
        var client = _clientFactory.CreateClient("API");
        var apiUrl = $"api/opportunities/{id}";

        try
        {
            // Load categories first
            ViewBag.Categories = await GetCategories(client);

            var response = await client.GetAsync(apiUrl);
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var opportunity = JsonSerializer.Deserialize<OpportunityViewModel>(content, _jsonOptions);

                if (opportunity != null)
                {
                    // The Category property is [JsonIgnore] so it won't be deserialized
                    // We need to manually set it if we have the data
                    if (opportunity.CategoryId.HasValue && !string.IsNullOrEmpty(opportunity.CategoryName))
                    {
                        opportunity.Category = new CategoryViewModel
                        {
                            CategoryID = opportunity.CategoryId.Value,
                            CategoryName = opportunity.CategoryName
                        };
                    }

                    // If we have a category but no name, try to get it from the ViewBag
                    if (opportunity.CategoryId.HasValue && string.IsNullOrEmpty(opportunity.CategoryName))
                    {
                        var categories = await GetCategories(client);
                        var category = categories?.FirstOrDefault(c => c.CategoryID == opportunity.CategoryId.Value);
                        if (category != null)
                        {
                            opportunity.Category = category;
                            opportunity.CategoryName = category.CategoryName;
                        }
                    }
                }

                return View(opportunity);
            }

            return HandleError(response.StatusCode);
        }
        catch (HttpRequestException)
        {
            return View("Error", new ErrorViewModel { RequestId = HttpContext.TraceIdentifier });
        }
    }

    // GET: Opportunities/Create
    [HttpGet("create")]
    public async Task<IActionResult> Create()
    {
        var client = _clientFactory.CreateClient("API");
        ViewBag.Categories = await GetCategories(client);
        return View(new CreateOpportunityViewModel
        {
            ApplicationDeadline = DateTime.Today.AddDays(30) // Default to 30 days from now
        });
    }

    // POST: Opportunities/Create
    [HttpPost("create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateOpportunityViewModel model)
    {
        if (!ModelState.IsValid)
        {
            var clientForCategories = _clientFactory.CreateClient("API");
            ViewBag.Categories = await GetCategories(clientForCategories);
            return View(model);
        }

        var client = await GetAuthenticatedClient();
        if (client == null)
        {
            return RedirectToAction("Login", "Auth");
        }

        try
        {
            var content = new StringContent(JsonSerializer.Serialize(model), Encoding.UTF8, "application/json");
            var response = await client.PostAsync("api/opportunities", content);

            if (response.IsSuccessStatusCode)
            {
                return RedirectToAction(nameof(Index));
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            ModelState.AddModelError(string.Empty, $"Error creating opportunity: {errorContent}");
            var clientForCategories = _clientFactory.CreateClient("API");
            ViewBag.Categories = await GetCategories(clientForCategories);
            return View(model);
        }
        catch (HttpRequestException)
        {
            return View("Error", new ErrorViewModel { RequestId = HttpContext.TraceIdentifier });
        }
    }

    // GET: Opportunities/Edit/5
    [HttpGet("edit/{id}")]
    public async Task<IActionResult> Edit(int id)
    {
        var client = await GetAuthenticatedClient();
        if (client == null)
        {
            return RedirectToAction("Login", "Auth");
        }

        try
        {
            var response = await client.GetAsync($"api/opportunities/{id}");
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var opportunity = JsonSerializer.Deserialize<OpportunityViewModel>(content, _jsonOptions);

                // Check if the current user is the owner of the opportunity
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (opportunity.PostedByUserId.ToString() != currentUserId)
                {
                    return Forbid();
                }

                // Load categories for the dropdown
                ViewBag.Categories = await GetCategories(client);

                var updateModel = new UpdateOpportunityViewModel
                {
                    Id = opportunity.Id,
                    Title = opportunity.Title,
                    Description = opportunity.Description,
                    Location = opportunity.Location,
                    IsRemote = opportunity.IsRemote,
                    Type = opportunity.Type,
                    ApplicationDeadline = opportunity.ApplicationDeadline,
                    TalentArea = opportunity.TalentArea,
                    CategoryId = opportunity.CategoryId
                };

                return View(updateModel);
            }

            return HandleError(response.StatusCode);
        }
        catch (HttpRequestException)
        {
            return View("Error", new ErrorViewModel { RequestId = HttpContext.TraceIdentifier });
        }
    }

    // POST: Opportunities/Edit/5
    [HttpPost("edit/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, UpdateOpportunityViewModel model)
    {
        if (id != model.Id)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var client = await GetAuthenticatedClient();
        if (client == null)
        {
            return RedirectToAction("Login", "Auth");
        }

        try
        {
            var content = new StringContent(JsonSerializer.Serialize(model), Encoding.UTF8, "application/json");
            var response = await client.PutAsync($"api/opportunities/{id}", content);

            if (response.IsSuccessStatusCode)
            {
                return RedirectToAction(nameof(Details), new { id });
            }

            ModelState.AddModelError(string.Empty, "Error updating opportunity. Please try again.");
            return View(model);
        }
        catch (HttpRequestException)
        {
            return View("Error", new ErrorViewModel { RequestId = HttpContext.TraceIdentifier });
        }
    }

    // POST: Opportunities/UpdateStatus/5
    [HttpPost("update-status/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(int id, [FromForm] string status)
    {
        var client = await GetAuthenticatedClient();
        if (client == null)
        {
            return RedirectToAction("Login", "Auth");
        }

        try
        {
            // Normalize the status (map 'close' to 'Closed', 'publish' to 'Open')
            var normalizedStatus = status.ToLower() switch
            {
                "close" => "Closed",
                "publish" => "Open",
                _ => status
            };

            // Log the status being sent
            _logger.LogInformation(
                $"Sending status update for opportunity {id}. Original status: {status}, Normalized status: {normalizedStatus}");

            // Send a POST request with status as a query parameter
            var response =
                await client.PostAsync(
                    $"api/opportunities/{id}/update-status?status={Uri.EscapeDataString(normalizedStatus)}", null);

            // Log the response status
            _logger.LogInformation($"Status update response: {response.StatusCode}");

            if (response.IsSuccessStatusCode)
            {
                // If we're on the details page, redirect back to details with a success message
                if (Request.Headers["Referer"].ToString().Contains("Details"))
                {
                    TempData["SuccessMessage"] = "Opportunity status updated successfully";
                    return RedirectToAction(nameof(Details), new { id });
                }

                // Otherwise, go to MyOpportunities with success message
                TempData["SuccessMessage"] = "Opportunity status updated successfully";
                return RedirectToAction(nameof(MyOpportunities));
            }

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                var errorMessage = "An error occurred while updating the application status.";

                try
                {
                    var errorResponse = JsonSerializer.Deserialize<Dictionary<string, string>>(errorContent);
                    if (errorResponse != null && errorResponse.ContainsKey("message"))
                    {
                        errorMessage = errorResponse["message"];
                    }
                }
                catch (Exception ex)
                {
                    // If we can't parse the error message, include the raw content
                    errorMessage = $"Error: {response.StatusCode} - {errorContent}";
                    _logger.LogError(ex, "Error parsing error response: {ErrorContent}", errorContent);
                }

                return StatusCode((int)response.StatusCode, new { message = errorMessage });
            }

            // Try to return to the previous page
            if (Request.Headers["Referer"].ToString().Contains("Details"))
            {
                return RedirectToAction(nameof(Details), new { id });
            }

            return RedirectToAction(nameof(MyOpportunities));
        }
        catch (HttpRequestException)
        {
            return View("Error", new ErrorViewModel { RequestId = HttpContext.TraceIdentifier });
        }
    }

    // GET: Opportunities/MyOpportunities
    [HttpGet("my-opportunities")]
    public async Task<IActionResult> MyOpportunities()
    {
        var client = await GetAuthenticatedClient();
        if (client == null)
        {
            return RedirectToAction("Login", "Auth");
        }

        try
        {
            var response = await client.GetAsync("api/opportunities/user");
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var opportunities = JsonSerializer.Deserialize<List<OpportunityViewModel>>(content, _jsonOptions);
                return View(opportunities);
            }

            return HandleError(response.StatusCode);
        }
        catch (HttpRequestException)
        {
            return View("Error", new ErrorViewModel { RequestId = HttpContext.TraceIdentifier });
        }
    }

    // POST: Opportunities/Delete/5
    [HttpPost("delete/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var client = await GetAuthenticatedClient();
        if (client == null)
        {
            return RedirectToAction("Login", "Auth");
        }

        try
        {
            var response = await client.DeleteAsync($"api/opportunities/{id}");
            if (response.IsSuccessStatusCode)
            {
                return RedirectToAction(nameof(MyOpportunities));
            }

            return HandleError(response.StatusCode);
        }
        catch (HttpRequestException)
        {
            return View("Error", new ErrorViewModel { RequestId = HttpContext.TraceIdentifier });
        }
    }

    // POST: Opportunities/Apply/5
    [HttpPost("apply/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Apply(int id, CreateOpportunityApplicationViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return RedirectToAction(nameof(Details), new { id });
        }

        var client = await GetAuthenticatedClient();
        if (client == null)
        {
            return RedirectToAction("Login", "Auth");
        }

        try
        {
            // Ensure the model has the correct opportunity ID
            model.TalentOpportunityID = id;

            var content = new StringContent(
                JsonSerializer.Serialize(model, _jsonOptions),
                Encoding.UTF8,
                "application/json");

            var response = await client.PostAsync($"api/opportunities/{id}/apply", content);

            if (response.IsSuccessStatusCode)
            {
                return RedirectToAction(nameof(MyApplications));
            }

            // Try to read the error message from the response
            var errorContent = await response.Content.ReadAsStringAsync();
            TempData["ErrorMessage"] = $"Error applying to opportunity: {errorContent}";
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"An error occurred: {ex.Message}";
            return RedirectToAction(nameof(Details), new { id });
        }
    }

    // GET: Opportunities/MyApplications
    [HttpGet("my-applications")]
    public async Task<IActionResult> MyApplications()
    {
        var client = await GetAuthenticatedClient();
        if (client == null)
        {
            return RedirectToAction("Login", "Auth");
        }

        try
        {
            var response = await client.GetAsync("api/opportunities/applications/me");
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var applications =
                    JsonSerializer.Deserialize<List<OpportunityApplicationViewModel>>(content, _jsonOptions);
                return View(applications);
            }

            return HandleError(response.StatusCode);
        }
        catch (HttpRequestException)
        {
            return View("Error", new ErrorViewModel { RequestId = HttpContext.TraceIdentifier });
        }
    }

    // GET: Opportunities/ApplicationDetails/5
    [HttpGet("application-details/{id}")]
    public async Task<IActionResult> ApplicationDetails(int id)
    {
        var client = await GetAuthenticatedClient();
        if (client == null)
        {
            return RedirectToAction("Login", "Auth");
        }

        try
        {
            // First get the application details
            var response = await client.GetAsync($"api/opportunities/applications/{id}");
            if (!response.IsSuccessStatusCode)
            {
                return HandleError(response.StatusCode);
            }

            var content = await response.Content.ReadAsStringAsync();
            var application = JsonSerializer.Deserialize<OpportunityApplicationViewModel>(content, _jsonOptions);

            if (application == null)
            {
                return NotFound();
            }

            // Check if the current user is the opportunity owner or the applicant
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (application.UserID.ToString() != currentUserId &&
                application.Opportunity?.PostedByUserId.ToString() != currentUserId)
            {
                return Forbid();
            }

            var categories = await GetCategories(client);

            return View(application);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting application details");
            return View("Error", new ErrorViewModel
            {
                RequestId = HttpContext.TraceIdentifier,
                Message = "An error occurred while loading the application details."
            });
        }
    }

    // GET: Opportunities/ManageApplications/5
    [HttpGet("manage-applications/{id}")]
    public async Task<IActionResult> ManageApplications(int id)
    {
        var client = await GetAuthenticatedClient();
        if (client == null)
        {
            return RedirectToAction("Login", "Auth");
        }

        try
        {
            // Get the opportunity details
            var opportunityResponse = await client.GetAsync($"api/opportunities/{id}");
            if (!opportunityResponse.IsSuccessStatusCode)
            {
                return HandleError(opportunityResponse.StatusCode);
            }

            var opportunityContent = await opportunityResponse.Content.ReadAsStringAsync();
            var opportunity = JsonSerializer.Deserialize<OpportunityViewModel>(opportunityContent, _jsonOptions);

            // Check if the current user is the owner of the opportunity
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (opportunity.PostedByUserId.ToString() != currentUserId)
            {
                return Forbid();
            }

            // Get applications for this opportunity with user data
            var applicationsResponse = await client.GetAsync($"api/opportunities/{id}/applications");
            if (!applicationsResponse.IsSuccessStatusCode)
            {
                return HandleError(applicationsResponse.StatusCode);
            }

            var applicationsContent = await applicationsResponse.Content.ReadAsStringAsync();
            var applications =
                JsonSerializer.Deserialize<List<OpportunityApplicationViewModel>>(applicationsContent, _jsonOptions);

            // Map the data to the view model
            foreach (var app in applications)
            {
                app.TalentOpportunityTitle = opportunity.Title;
                app.TalentOpportunityID = opportunity.Id;

                // Set the UserName for backward compatibility
                if (app.User != null && string.IsNullOrEmpty(app.UserName))
                {
                    app.UserName = app.User.FullName ?? app.User.Username;
                }
            }

            // Create the view model with applications
            var viewModel = new ManageApplicationsViewModel
            {
                OpportunityId = opportunity.Id,
                OpportunityTitle = opportunity.Title,
                Applications = applications.OrderByDescending(a => a.AppliedAt).ToList()
            };

            return View(viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading applications for management");
            return View("Error", new ErrorViewModel
            {
                RequestId = HttpContext.TraceIdentifier,
                Message = "An error occurred while loading applications."
            });
        }
    }

    // PUT: Opportunities/UpdateApplicationStatus/5/5
    [HttpPut("UpdateApplicationStatus/{opportunityId}/{applicationId}", Name = "UpdateApplicationStatus")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateApplicationStatus(int opportunityId, int applicationId,
        [FromBody] UpdateOpportunityApplicationViewModel model)
    {
        _logger.LogInformation(
            $"UpdateApplicationStatus called with opportunityId: {opportunityId}, applicationId: {applicationId}");
        _logger.LogInformation($"Model state is valid: {ModelState.IsValid}");

        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage);

            _logger.LogWarning($"Model validation failed: {string.Join(", ", errors)}");
            return BadRequest(new { message = "Invalid form data. " + string.Join(", ", errors) });
        }

        // Validate status value
        if (!model.TryGetStatus(out var status))
        {
            _logger.LogWarning($"Invalid status value: {model.Status}");
            return BadRequest(new { message = $"Invalid status value: {model.Status}" });
        }

        var client = await GetAuthenticatedClient();
        if (client == null)
        {
            return Unauthorized(new { message = "You must be logged in to perform this action." });
        }

        try
        {
            var content = new StringContent(
                JsonSerializer.Serialize(new
                {
                    status = model.Status,
                    reviewNotes = model.ReviewNotes
                }),
                Encoding.UTF8,
                "application/json");

            var response =
                await client.PutAsync($"api/opportunities/{opportunityId}/applications/{applicationId}/status",
                    content);

            if (response.IsSuccessStatusCode)
            {
                return Json(new { success = true, message = "Application status updated successfully." });
            }


            // Try to get error details from the response
            var errorContent = await response.Content.ReadAsStringAsync();
            var errorMessage = !string.IsNullOrEmpty(errorContent)
                ? $"Error: {errorContent}"
                : "An error occurred while updating the application status.";

            TempData["ErrorMessage"] = errorMessage;

            // If coming from the manage applications page, return there
            if (Request.Headers["Referer"].ToString().Contains("ManageApplications"))
            {
                return RedirectToAction(nameof(ManageApplications));
            }

            return RedirectToAction(nameof(ApplicationDetails), new { id = applicationId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating application status");
            TempData["ErrorMessage"] = $"An error occurred: {ex.Message}";

            // If coming from the manage applications page, return there
            if (Request.Headers["Referer"].ToString().Contains("ManageApplications"))
            {
                return RedirectToAction(nameof(ManageApplications));
            }

            return RedirectToAction(nameof(ApplicationDetails), new { id = applicationId });
        }
    }

    private async Task<List<CategoryViewModel>> GetCategories(HttpClient client)
    {
        try
        {
            var response = await client.GetAsync("api/categories");
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<List<CategoryViewModel>>(content, _jsonOptions) ??
                       new List<CategoryViewModel>();
            }

            return new List<CategoryViewModel>();
        }
        catch
        {
            return new List<CategoryViewModel>();
        }
    }

    private IActionResult HandleError(HttpStatusCode statusCode)
    {
        return statusCode switch
        {
            HttpStatusCode.NotFound => NotFound(),
            HttpStatusCode.Unauthorized => RedirectToAction("Login", "Auth"),
            HttpStatusCode.Forbidden => Forbid(),
            _ => StatusCode((int)statusCode)
        };
    }
}