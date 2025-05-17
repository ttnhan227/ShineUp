using Client.DTOs;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Text;

namespace Client.Controllers
{
    public class ContestEntriesController : Controller
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiBaseUrl = "https://localhost:7152/api";

        public ContestEntriesController()
        {
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Accept.Clear();
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        // GET: ContestEntries/Create
        public async Task<IActionResult> Create()
        {
            var contestsResponse = await _httpClient.GetAsync($"{_apiBaseUrl}/contests");
            var videosResponse = await _httpClient.GetAsync($"{_apiBaseUrl}/videos"); // assume exists

            var contests = new List<ContestDTO>();
            var videos = new List<VideoDTO>();

            if (contestsResponse.IsSuccessStatusCode)
            {
                var json = await contestsResponse.Content.ReadAsStringAsync();
                contests = JsonConvert.DeserializeObject<List<ContestDTO>>(json);
            }

            if (videosResponse.IsSuccessStatusCode)
            {
                var json = await videosResponse.Content.ReadAsStringAsync();
                videos = JsonConvert.DeserializeObject<List<VideoDTO>>(json);
            }

            ViewBag.Contests = contests;
            ViewBag.Videos = videos;

            return View();
        }

        // POST: ContestEntries/Create
        [HttpPost]
        public async Task<IActionResult> Create(ContestEntryDTO entry)
        {
            int userId = HttpContext.Session.GetInt32("UserID") ?? 0;
            entry.UserID = userId;

            var json = JsonConvert.SerializeObject(entry);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"{_apiBaseUrl}/contestentries", content);
            if (response.IsSuccessStatusCode)
            {
                return RedirectToAction("Index", "Contests");
            }

            ViewBag.Error = "Failed to submit contest entry.";
            return View(entry);
        }

    }
}
