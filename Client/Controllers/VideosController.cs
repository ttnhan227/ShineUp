using Client.Models;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;

namespace Client.Controllers
{
    // tạm thời để upload video test contest (anh)
    public class VideosController : Controller
    {
        private readonly HttpClient _client;

        public VideosController(IHttpClientFactory factory)
        {
            _client = factory.CreateClient("ShineUpApi");
        }

        [HttpGet]
        public IActionResult Upload()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Upload(Video model, IFormFile videoFile)
        {
            using var form = new MultipartFormDataContent();

            if (videoFile != null)
            {
                var stream = videoFile.OpenReadStream();
                form.Add(new StreamContent(stream), "VideoFile", videoFile.FileName);
            }

            form.Add(new StringContent(model.Title), "Title");
            form.Add(new StringContent(model.Description), "Description");
            form.Add(new StringContent(model.CategoryID.ToString()), "CategoryID");
            form.Add(new StringContent(model.PrivacyID.ToString()), "PrivacyID");

            var token = HttpContext.Session.GetString("AccessToken");
            if (!string.IsNullOrEmpty(token))
                _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var res = await _client.PostAsync("https://localhost:7152/videos/upload", form);

            if (res.IsSuccessStatusCode)
                return RedirectToAction("Index", "Home");

            return View(model);
        }
    }
}
