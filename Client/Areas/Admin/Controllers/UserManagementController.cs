using Client.Areas.Admin.Models;
using Client.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Text;

namespace Client.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class UserManagementController : Controller
{
    private readonly IHttpClientFactory _httpClientFactory;

    public UserManagementController(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<IActionResult> Index()
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
        {
            return Unauthorized();
        }

        var client = _httpClientFactory.CreateClient("API");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await client.GetAsync($"api/admin/UserManagement/{id}");
        if (!response.IsSuccessStatusCode)
        {
            return NotFound();
        }

        var json = await response.Content.ReadAsStringAsync();
        var user = JsonConvert.DeserializeObject<UserViewModel>(json);
        return View("~/Areas/Admin/Views/UserManagement/EditUser.cshtml", user);
    }

    [HttpPost]
    public async Task<IActionResult> EditUser(int id, EditUserRoleViewModel model)
    {
        var token = User.FindFirst("JWT")?.Value;
        if (string.IsNullOrEmpty(token))
        {
            return Unauthorized();
        }

        var client = _httpClientFactory.CreateClient("API");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var content = new StringContent(JsonConvert.SerializeObject(new { model.RoleID }), Encoding.UTF8,
            "application/json");
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
            return View("~/Areas/Admin/Views/UserManagement/EditUser.cshtml", user);
        }

        return RedirectToAction("Index");
    }

    [HttpPost]
    public async Task<IActionResult> UpdateUserStatus(int id, [FromBody] UpdateUserStatusViewModel model)
    {
        var token = User.FindFirst("JWT")?.Value;
        if (string.IsNullOrEmpty(token))
        {
            return Unauthorized();
        }

        var client = _httpClientFactory.CreateClient("API");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var content = new StringContent(
            JsonConvert.SerializeObject(new { field = model.Field, value = model.Value }),
            Encoding.UTF8,
            "application/json"
        );

        var response = await client.PutAsync($"api/admin/UserManagement/{id}/status", content);

        if (!response.IsSuccessStatusCode)
        {
            return Json(new { success = false, message = "Failed to update user status" });
        }

        return Json(new { success = true });
    }

    public async Task<IActionResult> DeleteUser(int id)
    {
        var token = User.FindFirst("JWT")?.Value;
        if (string.IsNullOrEmpty(token))
        {
            return Unauthorized();
        }

        var client = _httpClientFactory.CreateClient("API");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await client.DeleteAsync($"api/admin/UserManagement/{id}");
        // Optionally handle errors
        return RedirectToAction("Index");
    }
}