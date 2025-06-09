using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Client.Areas.Admin.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Client.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    [Route("Admin/[controller]/[action]")]
    public class ContestManagementController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly string _apiBaseUrl = "api/admin/ContestManagement";

        public ContestManagementController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        // GET: Admin/ContestManagement/Index
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var token = User.FindFirst("JWT")?.Value;
            if (string.IsNullOrEmpty(token))
                return Unauthorized();

            try
            {
                var client = GetAuthenticatedClient(token);
                // Update the endpoint to include entries with their vote counts
                var response = await client.GetAsync($"{_apiBaseUrl}?includeEntries=true");
                
                if (!response.IsSuccessStatusCode)
                    return View(new List<ContestViewModel>());

                var json = await response.Content.ReadAsStringAsync();
                var contests = JsonConvert.DeserializeObject<List<ContestViewModel>>(json);
                
                // Calculate total votes for each contest
                foreach (var contest in contests)
                {
                    contest.CalculateTotalVotes();
                }
                
                return View(contests);
            }
            catch (Exception ex)
            {
                // Log error
                ModelState.AddModelError("", "An error occurred while retrieving contests.");
                return View(new List<ContestViewModel>());
            }
        }

        // GET: Admin/ContestManagement/Details/5
        [HttpGet("{id}")]
        public async Task<IActionResult> Details(int id)
        {
            var token = User.FindFirst("JWT")?.Value;
            if (string.IsNullOrEmpty(token))
                return Unauthorized();

            try
            {
                var client = GetAuthenticatedClient(token);
                // Ensure we're including entries with their vote counts
                var response = await client.GetAsync($"{_apiBaseUrl}/{id}?includeEntries=true");
                
                if (!response.IsSuccessStatusCode)
                    return NotFound();

                var json = await response.Content.ReadAsStringAsync();
                var contest = JsonConvert.DeserializeObject<ContestDetailViewModel>(json);
                
                // Calculate total votes
                if (contest != null)
                {
                    contest.CalculateTotalVotes();
                }
                
                return View(contest);
            }
            catch (Exception ex)
            {
                // Log the exception
                ModelState.AddModelError("", "An error occurred while retrieving the contest details.");
                return View(new ContestDetailViewModel());
            }
        }

        // GET: Admin/ContestManagement/Create
        [HttpGet]
        public IActionResult Create()
        {
            return View(new CreateContestViewModel
            {
                StartDate = DateTime.Today,
                EndDate = DateTime.Today.AddDays(7)
            });
        }

        // POST: Admin/ContestManagement/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateContestViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var token = User.FindFirst("JWT")?.Value;
            if (string.IsNullOrEmpty(token))
                return Unauthorized();

            try
            {
                var client = GetAuthenticatedClient(token);
                var content = new StringContent(
                    JsonConvert.SerializeObject(model),
                    System.Text.Encoding.UTF8,
                    "application/json"
                );

                var response = await client.PostAsync(_apiBaseUrl, content);
                
                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    ModelState.AddModelError("", $"Failed to create contest: {error}");
                    return View(model);
                }

                return RedirectToAction(nameof(Index));
            }
            catch (Exception)
            {
                ModelState.AddModelError("", "An error occurred while creating the contest.");
                return View(model);
            }
        }

        // GET: Admin/ContestManagement/Edit/5
        [HttpGet("{id}")]
        public async Task<IActionResult> Edit(int id)
        {
            var token = User.FindFirst("JWT")?.Value;
            if (string.IsNullOrEmpty(token))
                return Unauthorized();

            try
            {
                var client = GetAuthenticatedClient(token);
                var response = await client.GetAsync($"{_apiBaseUrl}/{id}");
                
                if (!response.IsSuccessStatusCode)
                    return NotFound();

                var json = await response.Content.ReadAsStringAsync();
                var contest = JsonConvert.DeserializeObject<ContestDetailViewModel>(json);
                
                var editModel = new EditContestViewModel
                {
                    ContestID = contest.ContestID,
                    Title = contest.Title,
                    Description = contest.Description,
                    StartDate = contest.StartDate,
                    EndDate = contest.EndDate
                };

                return View(editModel);
            }
            catch (Exception)
            {
                ModelState.AddModelError("", "An error occurred while retrieving the contest.");
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Admin/ContestManagement/Edit/5
        [HttpPost("{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, EditContestViewModel model)
        {
            if (id != model.ContestID)
                return BadRequest();

            if (!ModelState.IsValid)
                return View(model);

            var token = User.FindFirst("JWT")?.Value;
            if (string.IsNullOrEmpty(token))
                return Unauthorized();

            try
            {
                var client = GetAuthenticatedClient(token);
                var content = new StringContent(
                    JsonConvert.SerializeObject(model),
                    System.Text.Encoding.UTF8,
                    "application/json"
                );

                var response = await client.PutAsync($"{_apiBaseUrl}/{id}", content);
                
                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    ModelState.AddModelError("", $"Failed to update contest: {error}");
                    return View(model);
                }

                return RedirectToAction(nameof(Index));
            }
            catch (Exception)
            {
                ModelState.AddModelError("", "An error occurred while updating the contest.");
                return View(model);
            }
        }

        // POST: Admin/ContestManagement/Delete/5
        [HttpPost("{id}"), ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var token = User.FindFirst("JWT")?.Value;
            if (string.IsNullOrEmpty(token))
                return Unauthorized();

            try
            {
                var client = GetAuthenticatedClient(token);
                var response = await client.DeleteAsync($"{_apiBaseUrl}/{id}");
                
                if (!response.IsSuccessStatusCode)
                {
                    TempData["ErrorMessage"] = "Failed to delete the contest.";
                    return RedirectToAction(nameof(Index));
                }

                return RedirectToAction(nameof(Index));
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "An error occurred while deleting the contest.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Admin/ContestManagement/DeleteEntry/5
        [HttpPost("entries/{entryId}"), ActionName("DeleteEntry")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteEntry(int entryId, [FromForm] int contestId)
        {
            var token = User.FindFirst("JWT")?.Value;
            if (string.IsNullOrEmpty(token))
                return Unauthorized();

            try
            {
                var client = GetAuthenticatedClient(token);
                var response = await client.DeleteAsync($"{_apiBaseUrl}/entries/{entryId}");
                
                if (!response.IsSuccessStatusCode)
                {
                    TempData["ErrorMessage"] = "Failed to delete the contest entry.";
                }

                return RedirectToAction(nameof(Details), new { id = contestId });
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "An error occurred while deleting the contest entry.";
                return RedirectToAction(nameof(Details), new { id = contestId });
            }
        }

        private HttpClient GetAuthenticatedClient(string token)
        {
            var client = _httpClientFactory.CreateClient("API");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return client;
        }
    }
}
