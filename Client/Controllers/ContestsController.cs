using Client.Models;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Text;

namespace Client.Controllers
{
    public class ContestsController : Controller
    {
        private readonly HttpClient _httpClient;
        private readonly IHttpContextAccessor _contextAccessor;

        public ContestsController(IHttpClientFactory httpClientFactory, IHttpContextAccessor contextAccessor)
        {
            _httpClient = httpClientFactory.CreateClient("API");
            _contextAccessor = contextAccessor;
        }

        // GET: /Contests
        public async Task<IActionResult> Index()
        {
            var response = await _httpClient.GetAsync("api/contests");
            if (!response.IsSuccessStatusCode) return View("Error");

            var contests = await response.Content.ReadFromJsonAsync<List<Contest>>();
            return View(contests);
        }

        // GET: /Contests/Details/{id}
        public async Task<IActionResult> Details(int id)
        {
            // 1. Get contest info
            var contestResponse = await _httpClient.GetAsync($"api/contests/{id}");
            if (!contestResponse.IsSuccessStatusCode) return View("Error");

            var contest = await contestResponse.Content.ReadFromJsonAsync<Contest>();

            // 2. Get entries for the contest
            var entriesResponse = await _httpClient.GetAsync($"api/contestentries/{id}");
            var entries = entriesResponse.IsSuccessStatusCode
                ? await entriesResponse.Content.ReadFromJsonAsync<List<ContestEntry>>()
                : new List<ContestEntry>();
            ViewBag.Entries = entries;

            // 3. Get list of entry IDs the current user has voted for
            List<int> votedEntryIds = new();
            var token = _contextAccessor.HttpContext?.Session.GetString("AccessToken");
            if (!string.IsNullOrEmpty(token))
            {
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var voteCheckRes = await _httpClient.GetAsync($"api/votes/votedentryids/{id}");
                if (voteCheckRes.IsSuccessStatusCode)
                    votedEntryIds = await voteCheckRes.Content.ReadFromJsonAsync<List<int>>();
            }
            ViewBag.VotedEntryIds = votedEntryIds;

            // 4. Get vote results for chart display
            var resultsResponse = await _httpClient.GetAsync($"api/votes/results/{id}");
            List<VoteResult> results = new();
            if (resultsResponse.IsSuccessStatusCode)
                results = await resultsResponse.Content.ReadFromJsonAsync<List<VoteResult>>();
            ViewBag.VoteResults = results;

            return View(contest);
        }
    }
}