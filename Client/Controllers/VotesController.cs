using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Text.Json;
using Client.Models;
using Microsoft.Extensions.Logging;

namespace Client.Controllers
{
    [Authorize]
    public class VotesController : Controller
    {
        private readonly IHttpClientFactory _clientFactory;
        private readonly ILogger<VotesController> _logger;
        private readonly JsonSerializerOptions _jsonOptions;

        public VotesController(IHttpClientFactory clientFactory, ILogger<VotesController> logger)
        {
            _clientFactory = clientFactory;
            _logger = logger;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Vote([FromBody] VoteRequest request)
        {
            try
            {
                var client = _clientFactory.CreateClient("API");
                
                // Get the auth token from cookies
                var token = HttpContext.Request.Cookies["auth_token"];
                if (!string.IsNullOrEmpty(token))
                {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                }

                // Forward the request to the server API
                var response = await client.PostAsJsonAsync("api/Votes", new { entryId = request.EntryId });
                
                if (!response.IsSuccessStatusCode)
                {
                    if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    {
                        return Json(new { success = false, message = "Please sign in to vote" });
                    }
                    
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Error from API: {StatusCode} - {Content}", response.StatusCode, errorContent);
                    return StatusCode((int)response.StatusCode, new { success = false, message = errorContent });
                }

                var content = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<VoteResponse>(content, _jsonOptions);
                
                return Json(new 
                { 
                    success = true,
                    entryId = request.EntryId,
                    voteCount = result.VoteCount,
                    hasVoted = result.HasVoted,
                    message = result.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing vote");
                return StatusCode(500, new { success = false, message = "An error occurred while processing your vote" });
            }
        }
    }
}
