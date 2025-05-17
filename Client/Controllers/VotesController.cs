
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Text;
using Client.DTOs;

namespace Client.Controllers
{
    public class VotesController : Controller
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiBaseUrl = "https://localhost:7152/api";

        public VotesController()
        {
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Accept.Clear();
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        // GET: Votes/Entries?contestId=1
        public async Task<IActionResult> Entries(int contestId)
        {
            var response = await _httpClient.GetAsync($"{_apiBaseUrl}/contestentries/contest/{contestId}");
            var entries = new List<dynamic>();

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                entries = JsonConvert.DeserializeObject<List<dynamic>>(json);
            }

            ViewBag.ContestId = contestId;
            return View(entries);
        }

        // POST: Votes/Cast
        [HttpPost]
        public async Task<IActionResult> Cast(int entryId, int contestId)
        {
            int userId = HttpContext.Session.GetInt32("UserID") ?? 0;

            var vote = new VoteDTO
            {
                EntryID = entryId,
                UserID = userId,
                VotedAt = DateTime.UtcNow
            };

            var json = JsonConvert.SerializeObject(vote);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"{_apiBaseUrl}/votes", content);

            TempData["Message"] = response.IsSuccessStatusCode
                ? "Vote successfully submitted!"
                : "Failed to vote. You might have already voted.";

            return RedirectToAction("Entries", new { contestId });
        }


        public async Task<IActionResult> Results(int contestId)
        {
            var response = await _httpClient.GetAsync($"{_apiBaseUrl}/votes/contest/{contestId}");
            var results = new List<dynamic>();

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                results = JsonConvert.DeserializeObject<List<dynamic>>(json);
            }

            ViewBag.Results = results;
            return View();
        }
    }
}
