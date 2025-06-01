using Client.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Text;

namespace Client.Controllers
{
    public class ContestEntriesController : Controller
    {
        private readonly HttpClient _httpClient;
        private readonly IHttpContextAccessor _context;

        public ContestEntriesController(IHttpClientFactory factory, IHttpContextAccessor contextAccessor)
        {
            _httpClient = factory.CreateClient("API");
            _context = contextAccessor;
        }

        // GET: /ContestEntries/Submit?contestId=5
        public async Task<IActionResult> Submit(int contestId)
        {
            ViewBag.ContestID = contestId;

            // 🔧 Tạm thời mock video (sau này gọi API videos/mine)
            var mockVideos = new List<Video>
            {
                new Video { VideoID = Guid.NewGuid(), Title = "Demo Video A" },
                new Video { VideoID = Guid.NewGuid(), Title = "Demo Video B" }
            };
            // thay mock sau
            ViewBag.Videos = new SelectList(mockVideos, "VideoID", "Title");

            return View(new ContestEntry
            {
                ContestID = contestId
            });
        }

        // POST: /ContestEntries/Submit
        [HttpPost]
        public async Task<IActionResult> Submit(ContestEntry entry)
        {
            var token = _context.HttpContext?.Session.GetString("AccessToken");
            if (string.IsNullOrEmpty(token))
            {
                TempData["Message"] = "You must be logged in to submit an entry.";
                return RedirectToAction("Index", "Contests");
            }

            // ⚠️ Không gửi UserID – để server tự lấy từ token
            entry.UserID = 0;

            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var response = await _httpClient.PostAsJsonAsync("api/contestentries", entry);

            if (response.IsSuccessStatusCode)
            {
                TempData["Message"] = "Entry submitted successfully!";
                return RedirectToAction("Details", "Contests", new { id = entry.ContestID });
            }

            ModelState.AddModelError("", "Submission failed. You may have already submitted.");
            return View(entry);
        }
    }
}