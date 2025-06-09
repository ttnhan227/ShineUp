using Microsoft.AspNetCore.Mvc;
using Client.Models;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Client.Controllers;

public class ContestsController : Controller
{
    private readonly IHttpClientFactory _clientFactory;
    private readonly ILogger<ContestsController> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ContestsController(
        IHttpClientFactory clientFactory, 
        ILogger<ContestsController> logger,
        IHttpContextAccessor httpContextAccessor)
    {
        _clientFactory = clientFactory;
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
    }
    
    private HttpClient CreateAuthenticatedClient()
    {
        var client = _clientFactory.CreateClient("API");
        
        var token = HttpContext.Request.Cookies["auth_token"];
        
        if (string.IsNullOrEmpty(token) && User.Identity is ClaimsIdentity identity)
        {
            var jwtClaim = identity.FindFirst("JWT");
            token = jwtClaim?.Value;
        }

        if (!string.IsNullOrEmpty(token))
        {
            client.DefaultRequestHeaders.Remove("Authorization");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        client.DefaultRequestHeaders.Accept.Clear();
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        return client;
    }

    public async Task<IActionResult> Index()
    {
        try
        {
            var client = _clientFactory.CreateClient("API");
            // First try to get token from cookie
            var token = HttpContext.Request.Cookies["auth_token"];
            
            // If not found in cookie, try to get from claims
            if (string.IsNullOrEmpty(token) && User.Identity.IsAuthenticated)
            {
                token = User.FindFirst("JWT")?.Value;
            }
            
            if (!string.IsNullOrEmpty(token))
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
            else if (User.Identity.IsAuthenticated)
            {
                _logger.LogWarning("JWT token not found in cookies or claims for authenticated user");
            }

            var response = await client.GetAsync("api/Contests");
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to get contests. Status: {StatusCode}", response.StatusCode);
                return View("Error");
            }

            var contests = await response.Content.ReadFromJsonAsync<List<ContestViewModel>>(new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            return View(contests);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting contests");
            return View("Error");
        }
    }

    public async Task<IActionResult> Details(int id)
    {
        try
        {
            var client = CreateAuthenticatedClient();
            var contestResponse = await client.GetAsync($"api/Contests/{id}");
            if (!contestResponse.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to get contest {ContestId}. Status: {StatusCode}", id, contestResponse.StatusCode);
                return NotFound();
            }

            var contest = await contestResponse.Content.ReadFromJsonAsync<ContestViewModel>(new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            var entriesResponse = await client.GetAsync($"api/ContestEntries/contest/{id}");
            var entries = entriesResponse.IsSuccessStatusCode
                ? await entriesResponse.Content.ReadFromJsonAsync<List<ContestEntryViewModel>>(new JsonSerializerOptions
                  {
                      PropertyNameCaseInsensitive = true
                  })
                : new List<ContestEntryViewModel>();

            ViewBag.Entries = entries;
            return View(contest);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting contest details for {ContestId}", id);
            return View("Error");
        }
    }

    [Authorize]
    public async Task<IActionResult> Submit(int id)
    {
        try
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new InvalidOperationException("User ID claim missing"));
            var client = CreateAuthenticatedClient();
            if (client.DefaultRequestHeaders.Authorization == null)
            {
                _logger.LogWarning("JWT token missing for contest submission");
                return RedirectToAction("Login", "Auth", new { returnUrl = Url.Action("Submit", "Contests", new { id }) });
            }

            var checkResponse = await client.GetAsync($"api/ContestEntries/user/{userId}/contest/{id}");
            if (checkResponse.IsSuccessStatusCode)
            {
                var check = await checkResponse.Content.ReadFromJsonAsync<Dictionary<string, bool>>();
                if (check["hasSubmitted"])
                {
                    ModelState.AddModelError("", "You have already submitted to this contest.");
                    return View("Error");
                }
            }
            else
            {
                _logger.LogError("Failed to check submission status. Status: {StatusCode}", checkResponse.StatusCode);
                return View("Error");
            }

            var model = new SubmitContestEntryViewModel 
            { 
                ContestID = id, 
                UserID = userId,
                MediaType = "video" // Default to video
            };
            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading contest submission form for {ContestId}", id);
            return View("Error");
        }
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(100 * 1024 * 1024)] // 100MB limit
    public async Task<IActionResult> Submit(SubmitContestEntryViewModel model)
    {
        if (!ModelState.IsValid)
        {
            _logger.LogWarning("SubmitContestEntry ModelState invalid");
            return View(model);
        }

        try
        {
            // Validate media file based on selected type
            if (!model.HasValidMedia)
            {
                ModelState.AddModelError("", $"Please provide a valid {model.MediaType} file");
                return View(model);
            }

            var client = CreateAuthenticatedClient();
            if (client.DefaultRequestHeaders.Authorization == null)
            {
                _logger.LogWarning("JWT token missing for contest submission");
                return RedirectToAction("Login", "Auth", new { returnUrl = Url.Action("Submit", "Contests", new { id = model.ContestID }) });
            }

            // Log the start of file upload
            _logger.LogInformation("Starting file upload for contest {ContestId}, file: {FileName}, size: {FileSize} bytes", 
                model.ContestID, 
                model.MediaType == "video" ? model.VideoFile?.FileName : model.ImageFile?.FileName,
                model.MediaType == "video" ? model.VideoFile?.Length : model.ImageFile?.Length);

            // Create multipart form data with boundary
            using var formData = new MultipartFormDataContent(Guid.NewGuid().ToString())
            {
                // Add basic fields
                { new StringContent(model.ContestID.ToString()), "ContestID" },
                { new StringContent(model.UserID.ToString()), "UserID" },
                { new StringContent(model.MediaType), "MediaType" },
                { new StringContent(model.Title ?? string.Empty), "Title" },
                { new StringContent(model.Description ?? string.Empty), "Description" }
            };
            
            try
            {
                // Add the appropriate file
                IFormFile fileToUpload = model.MediaType == "video" ? model.VideoFile! : model.ImageFile!;
                
                using var fileStream = fileToUpload.OpenReadStream();
                var fileContent = new StreamContent(fileStream);
                fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse(fileToUpload.ContentType);
                
                // Use a sanitized filename
                var fileName = Path.GetFileName(fileToUpload.FileName);
                formData.Add(fileContent, "file", fileName);
                
                // Log before sending the request
                _logger.LogDebug("Sending file upload request to API");
                
                // Send the request to the API with a timeout
                var cts = new CancellationTokenSource(TimeSpan.FromMinutes(10)); // 10 minute timeout
                var response = await client.PostAsync("api/ContestEntries/upload", formData, cts.Token);
                
                // Log the response status
                _logger.LogInformation("API response status: {StatusCode}", response.StatusCode);
                
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    _logger.LogInformation("Contest entry submitted successfully for contest {ContestId}", model.ContestID);
                    
                    TempData["SuccessMessage"] = "Your entry has been submitted successfully!";
                    return RedirectToAction("Details", new { id = model.ContestID });
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Failed to submit contest entry. Status: {StatusCode}, Response: {Response}", 
                        response.StatusCode, errorContent);
                    
                    ModelState.AddModelError("", "An error occurred while submitting your entry. Please try again.");
                    if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    {
                        return RedirectToAction("Login", "Auth", new { returnUrl = Url.Action("Submit", "Contests", new { id = model.ContestID }) });
                    }

                    // Handle specific error cases
                    if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
                    {
                        try
                        {
                            var errorObj = JsonDocument.Parse(errorContent, new JsonDocumentOptions());
                            if (errorObj.RootElement.TryGetProperty("errors", out var errors))
                            {
                                foreach (var error in errors.EnumerateObject())
                                {
                                    foreach (var errorMessage in error.Value.EnumerateArray())
                                    {
                                        ModelState.AddModelError(error.Name, errorMessage.GetString());
                                    }
                                }
                            }
                            else if (errorObj.RootElement.TryGetProperty("title", out var title))
                            {
                                ModelState.AddModelError("", title.GetString());
                            }
                            else
                            {
                                ModelState.AddModelError("", "An error occurred while submitting the entry.");
                            }
                        }
                        catch
                        {
                            ModelState.AddModelError("", errorContent);
                        }
                    }
                    else
                    {
                        ModelState.AddModelError("", "An error occurred while submitting the entry. Please try again later.");
                    }
                }
            }
            catch (OperationCanceledException ex)
            {
                _logger.LogError(ex, "File upload timed out for contest {ContestId}", model.ContestID);
                ModelState.AddModelError("", "The upload took too long. Please try again with a smaller file.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading file for contest {ContestId}", model.ContestID);
                ModelState.AddModelError("", "An error occurred while uploading your file. Please try again.");
            }
            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting contest entry for contest {ContestId}", model.ContestID);
            ModelState.AddModelError("", "An unexpected error occurred. Please try again.");
            return View(model);
        }
    }
}