using Client.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Client.Models.Admin;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Client.Controllers.Admin
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        public AdminController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public IActionResult Index()
        {
            return View();
        }

        public async Task<IActionResult> Users()
        {
            var token = User.FindFirst("JWT")?.Value;
            if (string.IsNullOrEmpty(token))
            {
                return Unauthorized();
            }
            var client = _httpClientFactory.CreateClient("API");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var response = await client.GetAsync("api/admin/UserManagement");
            if (!response.IsSuccessStatusCode)
            {
                return View(new List<UserViewModel>());
            }
            var json = await response.Content.ReadAsStringAsync();
            var users = JsonConvert.DeserializeObject<List<UserViewModel>>(json);
            return View(users);
        }

        public async Task<IActionResult> EditUser(int id)
        {
            var token = User.FindFirst("JWT")?.Value;
            if (string.IsNullOrEmpty(token))
                return Unauthorized();
            var client = _httpClientFactory.CreateClient("API");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var response = await client.GetAsync($"api/admin/UserManagement/{id}");
            if (!response.IsSuccessStatusCode)
                return NotFound();
            var json = await response.Content.ReadAsStringAsync();
            var user = JsonConvert.DeserializeObject<UserViewModel>(json);
            return View(user);
        }

        [HttpPost]
        public async Task<IActionResult> EditUser(int id, EditUserRoleViewModel model)
        {
            var token = User.FindFirst("JWT")?.Value;
            if (string.IsNullOrEmpty(token))
                return Unauthorized();
            var client = _httpClientFactory.CreateClient("API");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var content = new StringContent(JsonConvert.SerializeObject(new { RoleID = model.RoleID }), System.Text.Encoding.UTF8, "application/json");
            var response = await client.PutAsync($"api/admin/UserManagement/{id}/role", content);
            if (!response.IsSuccessStatusCode)
            {
                // Fetch the user again to get the correct model for the view
                var userResponse = await client.GetAsync($"api/admin/UserManagement/{id}");
                UserViewModel user = null;
                if (userResponse.IsSuccessStatusCode)
                {
                    var json = await userResponse.Content.ReadAsStringAsync();
                    user = JsonConvert.DeserializeObject<UserViewModel>(json);
                }
                ModelState.AddModelError("", "Failed to update user role.");
                return View(user);
            }
            return RedirectToAction("Users");
        }

        public async Task<IActionResult> DeleteUser(int id)
        {
            var token = User.FindFirst("JWT")?.Value;
            if (string.IsNullOrEmpty(token))
                return Unauthorized();
            var client = _httpClientFactory.CreateClient("API");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var response = await client.DeleteAsync($"api/admin/UserManagement/{id}");
            // Optionally handle errors
            return RedirectToAction("Users");
        }
    }
}
