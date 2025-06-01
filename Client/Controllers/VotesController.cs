using Client.Models;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Text;

namespace Client.Controllers
{
    public class VotesController : Controller
    {
        private readonly HttpClient _httpClient;
        private readonly IHttpContextAccessor _context;

        public VotesController(IHttpClientFactory factory, IHttpContextAccessor contextAccessor)
        {
            _httpClient = factory.CreateClient("API");
            _context = contextAccessor;
        }

        // POST: /Votes/Vote
        [HttpPost]
        public async Task<IActionResult> Vote(int contestId, Vote vote)
        {
            var token = _context.HttpContext?.Session.GetString("AccessToken");
            if (string.IsNullOrEmpty(token))
            {
                TempData["Message"] = "Please login before voting.";
                return RedirectToAction("Details", "Contests", new { id = contestId });
            }

            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.PostAsJsonAsync("api/votes", vote);

            TempData["Message"] = response.IsSuccessStatusCode
                ? "Vote submitted successfully!"
                : "You have already voted or an error occurred.";

            return RedirectToAction("Details", "Contests", new { id = contestId });
        }

        // GET: /Votes/Results/{contestId}
        public async Task<IActionResult> Results(int contestId)
        {
            var response = await _httpClient.GetAsync($"api/votes/results/{contestId}");
            if (!response.IsSuccessStatusCode)
                return View("Error");

            var results = await response.Content.ReadFromJsonAsync<List<VoteResult>>();
            return View(results);
        }
    }
}