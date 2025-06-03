using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Client.Models;
using System.Text.Json;
using System.Text;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace Client.Controllers;

[Authorize]
[Route("opportunities")]
public class OpportunitiesController : Controller
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<OpportunitiesController> _logger;
    private readonly IConfiguration _configuration;

    public OpportunitiesController(
        IHttpClientFactory httpClientFactory,
        ILogger<OpportunitiesController> logger,
        IConfiguration configuration)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _configuration = configuration;
    }

    private HttpClient CreateAuthenticatedClient()
    {
        var client = _httpClientFactory.CreateClient("API");
        var token = HttpContext.Request.Cookies["auth_token"];
        if (!string.IsNullOrEmpty(token))
        {
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        }
        return client;
    }

    // GET: /opportunities
    [AllowAnonymous]
    public async Task<IActionResult> Index()
    {
        try
        {
            var client = CreateAuthenticatedClient();
            var response = await client.GetAsync("api/opportunities");
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError($"Failed to fetch opportunities. Status: {response.StatusCode}");
                return View("Error");
            }

            var content = await response.Content.ReadAsStringAsync();
            var opportunities = JsonSerializer.Deserialize<List<OpportunityViewModel>>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return View(opportunities);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching opportunities");
            return View("Error");
        }
    }

    // GET: /opportunities/5
    [AllowAnonymous]
    [Route("{id}")]
    public async Task<IActionResult> Details(int id)
    {
        try
        {
            var client = CreateAuthenticatedClient();
            var response = await client.GetAsync($"api/opportunities/{id}");
            
            if (!response.IsSuccessStatusCode)
            {
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return NotFound();
                }
                return View("Error");
            }

            var content = await response.Content.ReadAsStringAsync();
            var opportunity = JsonSerializer.Deserialize<OpportunityViewModel>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return View(opportunity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error fetching opportunity with ID {id}");
            return View("Error");
        }
    }

    // GET: /opportunities/create
    [Route("create")]
    public IActionResult Create()
    {
        return View(new CreateOpportunityViewModel());
    }

    // POST: /opportunities/create
    [HttpPost("create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateOpportunityViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            var client = CreateAuthenticatedClient();
            var content = new StringContent(
                JsonSerializer.Serialize(model),
                Encoding.UTF8,
                "application/json");

            var response = await client.PostAsync("api/opportunities", content);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError($"Failed to create opportunity. Status: {response.StatusCode}, Error: {errorContent}");
                ModelState.AddModelError(string.Empty, "An error occurred while creating the opportunity.");
                return View(model);
            }

            var createdContent = await response.Content.ReadAsStringAsync();
            var createdOpportunity = JsonSerializer.Deserialize<OpportunityViewModel>(
                createdContent, 
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return RedirectToAction(nameof(Details), new { id = createdOpportunity?.Id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating opportunity");
            ModelState.AddModelError(string.Empty, "An error occurred while creating the opportunity.");
            return View(model);
        }
    }

    // GET: /opportunities/5/edit
    [Route("{id}/edit")]
    public async Task<IActionResult> Edit(int id)
    {
        try
        {
            var client = CreateAuthenticatedClient();
            var response = await client.GetAsync($"api/opportunities/{id}");
            
            if (!response.IsSuccessStatusCode)
            {
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return NotFound();
                }
                return View("Error");
            }

            var content = await response.Content.ReadAsStringAsync();
            var opportunity = JsonSerializer.Deserialize<OpportunityViewModel>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            var editModel = new UpdateOpportunityViewModel
            {
                Id = opportunity.Id,
                Title = opportunity.Title,
                Description = opportunity.Description,
                Location = opportunity.Location,
                IsRemote = opportunity.IsRemote,
                Type = opportunity.Type,
                Status = opportunity.Status,
                ApplicationDeadline = opportunity.ApplicationDeadline,
                CategoryId = opportunity.CategoryId,
                TalentArea = opportunity.TalentArea
            };

            return View(editModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error loading opportunity for edit with ID {id}");
            return View("Error");
        }
    }

    // POST: /opportunities/5/edit
    [HttpPost("{id}/edit")]
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

        try
        {
            var client = CreateAuthenticatedClient();
            var content = new StringContent(
                JsonSerializer.Serialize(model),
                Encoding.UTF8,
                "application/json");

            var response = await client.PutAsync($"api/opportunities/{id}", content);
            
            if (!response.IsSuccessStatusCode)
            {
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return NotFound();
                }
                
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError($"Failed to update opportunity. Status: {response.StatusCode}, Error: {errorContent}");
                ModelState.AddModelError(string.Empty, "An error occurred while updating the opportunity.");
                return View(model);
            }

            return RedirectToAction(nameof(Details), new { id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error updating opportunity with ID {id}");
            ModelState.AddModelError(string.Empty, "An error occurred while updating the opportunity.");
            return View(model);
        }
    }

    // GET: /opportunities/my
    [Route("my")]
    public async Task<IActionResult> MyOpportunities()
    {
        try
        {
            var client = CreateAuthenticatedClient();
            var response = await client.GetAsync("api/opportunities/user");
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError($"Failed to fetch user's opportunities. Status: {response.StatusCode}");
                return View("Error");
            }

            var content = await response.Content.ReadAsStringAsync();
            var opportunities = JsonSerializer.Deserialize<List<OpportunityViewModel>>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return View(opportunities);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching user's opportunities");
            return View("Error");
        }
    }

    // GET: /opportunities/talent/music
    [AllowAnonymous]
    [Route("talent/{talentArea}")]
    public async Task<IActionResult> ByTalentArea(string talentArea)
    {
        try
        {
            var client = CreateAuthenticatedClient();
            var response = await client.GetAsync($"api/opportunities/talent/{talentArea}");
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError($"Failed to fetch opportunities for talent area {talentArea}. Status: {response.StatusCode}");
                return View("Error");
            }

            var content = await response.Content.ReadAsStringAsync();
            var opportunities = JsonSerializer.Deserialize<List<OpportunityViewModel>>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            ViewBag.TalentArea = talentArea;
            return View(opportunities);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error fetching opportunities for talent area {talentArea}");
            return View("Error");
        }
    }

    // POST: /opportunities/5/apply
    [HttpPost("{opportunityId}/apply")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Apply(int opportunityId, CreateOpportunityApplicationViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return RedirectToAction(nameof(Details), new { id = opportunityId });
        }

        try
        {
            var client = CreateAuthenticatedClient();
            model.TalentOpportunityID = opportunityId;
            
            var content = new StringContent(
                JsonSerializer.Serialize(model),
                Encoding.UTF8,
                "application/json");

            var response = await client.PostAsync($"api/opportunities/{opportunityId}/apply", content);
            
            if (!response.IsSuccessStatusCode)
            {
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return NotFound();
                }
                
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError($"Failed to apply for opportunity. Status: {response.StatusCode}, Error: {errorContent}");
                TempData["ErrorMessage"] = "An error occurred while applying for the opportunity.";
                return RedirectToAction(nameof(Details), new { id = opportunityId });
            }

            TempData["SuccessMessage"] = "Your application has been submitted successfully!";
            return RedirectToAction(nameof(Details), new { id = opportunityId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error applying for opportunity with ID {opportunityId}");
            TempData["ErrorMessage"] = "An error occurred while applying for the opportunity.";
            return RedirectToAction(nameof(Details), new { id = opportunityId });
        }
    }

    // GET: /opportunities/5/applications
    [Route("{opportunityId}/applications")]
    public async Task<IActionResult> Applications(int opportunityId)
    {
        try
        {
            var client = CreateAuthenticatedClient();
            var response = await client.GetAsync($"api/opportunities/{opportunityId}/applications");
            
            if (!response.IsSuccessStatusCode)
            {
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return NotFound();
                }
                return View("Error");
            }

            var content = await response.Content.ReadAsStringAsync();
            var applications = JsonSerializer.Deserialize<List<OpportunityApplicationViewModel>>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            // Get opportunity details for the view
            var opportunityResponse = await client.GetAsync($"api/opportunities/{opportunityId}");
            if (opportunityResponse.IsSuccessStatusCode)
            {
                var opportunityContent = await opportunityResponse.Content.ReadAsStringAsync();
                var opportunity = JsonSerializer.Deserialize<OpportunityViewModel>(
                    opportunityContent, 
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                
                ViewBag.Opportunity = opportunity;
            }

            return View(applications);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error fetching applications for opportunity with ID {opportunityId}");
            return View("Error");
        }
    }

    // POST: /opportunities/5/applications/3/status
    [HttpPost("{opportunityId}/applications/{applicationId}/status")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateApplicationStatus(
        int opportunityId, 
        int applicationId, 
        UpdateOpportunityApplicationViewModel model)
    {
        if (applicationId != model.ApplicationID)
        {
            return BadRequest();
        }

        try
        {
            var client = CreateAuthenticatedClient();
            var content = new StringContent(
                JsonSerializer.Serialize(model),
                Encoding.UTF8,
                "application/json");

            var response = await client.PutAsync(
                $"api/opportunities/{opportunityId}/applications/{applicationId}/status", 
                content);
            
            if (!response.IsSuccessStatusCode)
            {
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return NotFound();
                }
                
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError($"Failed to update application status. Status: {response.StatusCode}, Error: {errorContent}");
                TempData["ErrorMessage"] = "An error occurred while updating the application status.";
                return RedirectToAction(nameof(Applications), new { opportunityId });
            }

            TempData["SuccessMessage"] = "Application status updated successfully!";
            return RedirectToAction(nameof(Applications), new { opportunityId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error updating application status for application ID {applicationId}");
            TempData["ErrorMessage"] = "An error occurred while updating the application status.";
            return RedirectToAction(nameof(Applications), new { opportunityId });
        }
    }
}
