using Microsoft.AspNetCore.Mvc;
using Client.Models;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Client.Controllers;

public class ContestsController : Controller
{
    private readonly IHttpClientFactory _clientFactory;
    private readonly ILogger<ContestsController> _logger;

    public ContestsController(IHttpClientFactory clientFactory, ILogger<ContestsController> logger)
    {
        _clientFactory = clientFactory;
        _logger = logger;
    }

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

            var response = await client.GetAsync("api/Contests");
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to get contests. Status: {StatusCode}", response.StatusCode);
                return View("Error");
            }

            var contests = await response.Content.ReadFromJsonAsync<List<ContestViewModel>>(new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            return View(contests);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting contests");
            return View("Error");
        }
    }

    public async Task<IActionResult> Details(int id)
    {
        try
        {
            var client = _clientFactory.CreateClient("API");
            var token = HttpContext.Request.Cookies["auth_token"];
            if (!string.IsNullOrEmpty(token))
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            var contestResponse = await client.GetAsync($"api/Contests/{id}");
            if (!contestResponse.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to get contest {ContestId}. Status: {StatusCode}", id, contestResponse.StatusCode);
                return NotFound();
            }

            var contest = await contestResponse.Content.ReadFromJsonAsync<ContestViewModel>(new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            var entriesResponse = await client.GetAsync($"api/ContestEntries/contest/{id}");
            var entries = entriesResponse.IsSuccessStatusCode
                ? await entriesResponse.Content.ReadFromJsonAsync<List<ContestEntryViewModel>>(new JsonSerializerOptions
                  {
                      PropertyNameCaseInsensitive = true
                  })
                : new List<ContestEntryViewModel>();

            ViewBag.Entries = entries;
            return View(contest);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting contest details for {ContestId}", id);
            return View("Error");
        }
    }

    [Authorize]
    public async Task<IActionResult> Submit(int id)
    {
        try
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new InvalidOperationException("User ID claim missing"));
            var client = _clientFactory.CreateClient("API");
            var token = HttpContext.Request.Cookies["auth_token"];
            if (string.IsNullOrEmpty(token))
            {
                _logger.LogWarning("JWT token missing for contest submission");
                return RedirectToAction("Login", "Auth", new { returnUrl = Url.Action("Submit", "Contests", new { id }) });
            }

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var checkResponse = await client.GetAsync($"api/ContestEntries/user/{userId}/contest/{id}");
            if (checkResponse.IsSuccessStatusCode)
            {
                var check = await checkResponse.Content.ReadFromJsonAsync<Dictionary<string, bool>>();
                if (check["hasSubmitted"])
                {
                    ModelState.AddModelError("", "You have already submitted to this contest.");
                    return View("Error");
                }
            }
            else
            {
                _logger.LogError("Failed to check submission status. Status: {StatusCode}", checkResponse.StatusCode);
                return View("Error");
            }

            var model = new SubmitContestEntryViewModel { ContestID = id, UserID = userId };
            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading contest submission form for {ContestId}", id);
            return View("Error");
        }
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Submit(SubmitContestEntryViewModel model)
    {
        if (!ModelState.IsValid)
        {
            _logger.LogWarning("SubmitContestEntry ModelState invalid");
            return View(model);
        }

        try
        {
            var client = _clientFactory.CreateClient("API");
            var token = HttpContext.Request.Cookies["auth_token"];
            if (string.IsNullOrEmpty(token))
            {
                _logger.LogWarning("JWT token missing for contest submission");
                return RedirectToAction("Login", "Auth", new { returnUrl = Url.Action("Submit", "Contests", new { id = model.ContestID }) });
            }

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await client.PostAsJsonAsync("api/ContestEntries", model);
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Contest entry submitted successfully for contest {ContestId}", model.ContestID);
                return RedirectToAction("Details", new { id = model.ContestID });
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            _logger.LogError("Failed to submit contest entry. Status: {StatusCode}, Error: {Error}", response.StatusCode, errorContent);
            ModelState.AddModelError("", $"Failed to submit entry: {errorContent}");
            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting contest entry for {ContestId}", model.ContestID);
            ModelState.AddModelError("", "An error occurred while submitting the entry");
            return View(model);
        }
    }
}