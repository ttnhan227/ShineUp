using Microsoft.AspNetCore.Mvc;
using Client.Models;
using Microsoft.AspNetCore.Authentication;
using System.Threading.Tasks;
using System.Net.Http;
using System.Text;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace Client.Controllers
{
    [Authorize]
    public class UserProfileController : Controller
    {
        private readonly HttpClient _httpClient;

        public UserProfileController(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("User ID not found in token.");
            }

            var token = User.FindFirst("JWT")?.Value;
            if (string.IsNullOrEmpty(token))
            {
                return Unauthorized("JWT token not found.");
            }

            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.GetAsync($"api/UserProfile/{userId}");

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var userProfile = JsonConvert.DeserializeObject<UserViewModel>(content);
                return View(userProfile);
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return NotFound("User profile not found.");
            }
            else
            {
                ModelState.AddModelError("", "Error fetching user profile.");
                return View(new UserViewModel()); // Return an empty model or handle error appropriately
            }
        }

        [HttpGet]
        public async Task<IActionResult> Edit()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("User ID not found in token.");
            }

            var token = User.FindFirst("JWT")?.Value;
            if (string.IsNullOrEmpty(token))
            {
                return Unauthorized("JWT token not found.");
            }

            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.GetAsync($"api/UserProfile/{userId}");

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var userProfile = JsonConvert.DeserializeObject<UserViewModel>(content);
                
                var profileViewModel = new ProfileViewModel
                {
                    Username = userProfile.Username,
                    Email = userProfile.Email,
                    Bio = userProfile.Bio,
                    ProfileImageURL = userProfile.ProfileImageURL,
                    TalentArea = userProfile.TalentArea
                };
                return View(profileViewModel);
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return NotFound("User profile not found.");
            }
            else
            {
                ModelState.AddModelError("", "Error fetching user profile for editing.");
                return View(new ProfileViewModel());
            }
        }

        [HttpPost]
        public async Task<IActionResult> Edit(ProfileViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("User ID not found in token.");
            }

            var token = User.FindFirst("JWT")?.Value;
            if (string.IsNullOrEmpty(token))
            {
                return Unauthorized("JWT token not found.");
            }

            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            using var formData = new MultipartFormDataContent();

            // Add string content
            formData.Add(new StringContent(model.Username ?? string.Empty), "Username");
            formData.Add(new StringContent(model.Email ?? string.Empty), "Email");
            formData.Add(new StringContent(model.Bio ?? string.Empty), "Bio");
            formData.Add(new StringContent(model.TalentArea ?? string.Empty), "TalentArea");
            
            // Add existing ProfileImageURL if it's not being replaced
            if (!string.IsNullOrEmpty(model.ProfileImageURL) && model.ProfileImageFile == null)
            {
                formData.Add(new StringContent(model.ProfileImageURL), "ProfileImageUrl");
            }

            // Add file content if a new file is uploaded
            if (model.ProfileImageFile != null)
            {
                var fileStreamContent = new StreamContent(model.ProfileImageFile.OpenReadStream());
                fileStreamContent.Headers.ContentType = new MediaTypeHeaderValue(model.ProfileImageFile.ContentType);
                formData.Add(fileStreamContent, "ProfileImageFile", model.ProfileImageFile.FileName);
            }

            var response = await _httpClient.PutAsync($"api/UserProfile/{userId}", formData); // Send formData

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var updatedUser = JsonConvert.DeserializeObject<UserViewModel>(responseContent);

                // Update the claims with new profile image URL
                var identity = new ClaimsIdentity(User.Claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var existingClaim = identity.FindFirst("ProfileImageURL");
                if (existingClaim != null)
                {
                    identity.RemoveClaim(existingClaim);
                }
                identity.AddClaim(new Claim("ProfileImageURL", updatedUser.ProfileImageURL ?? "https://via.placeholder.com/30/007bff/FFFFFF?text=U"));

                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));

                TempData["SuccessMessage"] = "Profile updated successfully!";
                return RedirectToAction("Index");
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                ModelState.AddModelError("", $"Error updating profile: {errorContent}");
                return View(model);
            }
        }

    }
}
