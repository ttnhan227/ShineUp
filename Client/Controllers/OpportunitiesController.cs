using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using Client.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using System.Text.Json.Serialization;
using System.Linq;

namespace Client.Controllers
{
    [Authorize]
    public class OpportunitiesController : Controller
    {
        private readonly IHttpClientFactory _clientFactory;
        private readonly IConfiguration _configuration;
        private readonly JsonSerializerOptions _jsonOptions;
        private readonly ILogger<OpportunitiesController> _logger;

        public OpportunitiesController(
            IHttpClientFactory clientFactory,
            IConfiguration configuration,
            ILogger<OpportunitiesController> logger)
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
                var response = await client.GetAsync(apiUrl);
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var opportunity = JsonSerializer.Deserialize<OpportunityViewModel>(content, _jsonOptions);
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
        public IActionResult Create()
        {
            return View();
        }

        // POST: Opportunities/Create
        [HttpPost("create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateOpportunityViewModel model)
        {
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
                var content = new StringContent(JsonSerializer.Serialize(model), System.Text.Encoding.UTF8, "application/json");
                var response = await client.PostAsync("api/opportunities", content);
                
                if (response.IsSuccessStatusCode)
                {
                    return RedirectToAction(nameof(Index));
                }
                
                ModelState.AddModelError(string.Empty, "Error creating opportunity. Please try again.");
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

                    var updateModel = new UpdateOpportunityViewModel
                    {
                        Id = opportunity.Id,
                        Title = opportunity.Title,
                        Description = opportunity.Description,
                        Location = opportunity.Location,
                        IsRemote = opportunity.IsRemote,
                        Type = opportunity.Type,
                        ApplicationDeadline = opportunity.ApplicationDeadline,
                        TalentArea = opportunity.TalentArea
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
                var content = new StringContent(JsonSerializer.Serialize(model), System.Text.Encoding.UTF8, "application/json");
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
                var updateModel = new UpdateOpportunityViewModel();
                
                switch (status.ToLower())
                {
                    case "pause":
                        updateModel.Status = OpportunityStatus.Closed;
                        break;
                    case "publish":
                        updateModel.Status = OpportunityStatus.Open;
                        break;
                    default:
                        return BadRequest("Invalid status");
                }


                var content = new StringContent(JsonSerializer.Serialize(updateModel, _jsonOptions), 
                    System.Text.Encoding.UTF8, "application/json");
                    
                var response = await client.PutAsync($"api/opportunities/{id}", content);
                
                if (response.IsSuccessStatusCode)
                {
                    return RedirectToAction(nameof(MyOpportunities));
                }
                
                TempData["ErrorMessage"] = "Error updating opportunity status. Please try again.";
                return RedirectToAction(nameof(Details), new { id });
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
                var content = new StringContent(JsonSerializer.Serialize(model), System.Text.Encoding.UTF8, "application/json");
                var response = await client.PostAsync($"api/opportunities/{id}/apply", content);
                
                if (response.IsSuccessStatusCode)
                {
                    return RedirectToAction(nameof(MyApplications));
                }
                
                TempData["ErrorMessage"] = "Error applying to opportunity. Please try again.";
                return RedirectToAction(nameof(Details), new { id });
            }
            catch (HttpRequestException)
            {
                return View("Error", new ErrorViewModel { RequestId = HttpContext.TraceIdentifier });
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
                    var applications = JsonSerializer.Deserialize<List<OpportunityApplicationViewModel>>(content, _jsonOptions);
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
                // This is a simplified version - you might need to adjust based on your API
                var response = await client.GetAsync($"api/opportunities/applications/{id}");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var application = JsonSerializer.Deserialize<OpportunityApplicationViewModel>(content, _jsonOptions);
                    return View(application);
                }
                return HandleError(response.StatusCode);
            }
            catch (HttpRequestException)
            {
                return View("Error", new ErrorViewModel { RequestId = HttpContext.TraceIdentifier });
            }
        }

        // POST: Opportunities/UpdateApplicationStatus/5
        [HttpPost("update-application-status/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateApplicationStatus(int id, UpdateOpportunityApplicationViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return RedirectToAction(nameof(ApplicationDetails), new { id });
            }

            var client = await GetAuthenticatedClient();
            if (client == null)
            {
                return RedirectToAction("Login", "Auth");
            }
            
            try
            {
                var content = new StringContent(JsonSerializer.Serialize(model), System.Text.Encoding.UTF8, "application/json");
                var response = await client.PutAsync($"api/opportunities/applications/{id}/status", content);
                
                if (response.IsSuccessStatusCode)
                {
                    return RedirectToAction(nameof(MyOpportunities));
                }
                
                TempData["ErrorMessage"] = "Error updating application status. Please try again.";
                return RedirectToAction(nameof(ApplicationDetails), new { id });
            }
            catch (HttpRequestException)
            {
                return View("Error", new ErrorViewModel { RequestId = HttpContext.TraceIdentifier });
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
                    return JsonSerializer.Deserialize<List<CategoryViewModel>>(content, _jsonOptions) ?? new List<CategoryViewModel>();
                }
                return new List<CategoryViewModel>();
            }
            catch
            {
                return new List<CategoryViewModel>();
            }
        }

        private IActionResult HandleError(System.Net.HttpStatusCode statusCode)
        {
            return statusCode switch
            {
                System.Net.HttpStatusCode.NotFound => NotFound(),
                System.Net.HttpStatusCode.Unauthorized => RedirectToAction("Login", "Auth"),
                System.Net.HttpStatusCode.Forbidden => Forbid(),
                _ => StatusCode((int)statusCode)
            };
        }
    }
}
