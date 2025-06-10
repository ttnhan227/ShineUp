using Client.Service;
using Microsoft.AspNetCore.Mvc;

namespace Client.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ChatBotController : ControllerBase
{
    private readonly OpenRouterService _openRouterService;

    public ChatBotController(OpenRouterService openRouterService)
    {
        _openRouterService = openRouterService;
    }

    [HttpPost]
    public async Task<IActionResult> Chat([FromBody] ChatMessageDto request)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
        {
            return BadRequest("Message is required.");
        }

        try
        {
            var reply = await _openRouterService.AskAsync(request.Message);
            return Ok(new { message = reply });
        }
        catch (Exception ex)
        {
            // Log thực tế tại đây nếu cần (Serilog, NLog, etc.)
            return StatusCode(500, $"Chatbot internal error: {ex.Message}");
        }
    }
}

public class ChatMessageDto
{
    public string Message { get; set; }
}