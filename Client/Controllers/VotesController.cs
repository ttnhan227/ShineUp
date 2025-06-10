using Client.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;

namespace Client.Controllers;

[Authorize]
public class VotesController : Controller
{
    private readonly IHttpClientFactory _clientFactory;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly ILogger<VotesController> _logger;

    public VotesController(
        IHttpClientFactory clientFactory,
        ILogger<VotesController> logger,
        IHttpContextAccessor httpContextAccessor)
    {
        _clientFactory = clientFactory;
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
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

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Vote([FromBody] VoteRequest request)
    {
        try
        {
            var client = CreateAuthenticatedClient();
            if (client.DefaultRequestHeaders.Authorization == null)
            {
                return Json(new { success = false, message = "Please sign in to vote" });
            }

            // Forward the request to the server API
            var response = await client.PostAsJsonAsync("api/Votes", new { entryId = request.EntryId });

            if (!response.IsSuccessStatusCode)
            {
                if (response.StatusCode == HttpStatusCode.Unauthorized)
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