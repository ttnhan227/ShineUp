using Client.Service;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace Client.Controllers
{
    [Route("ChatBot")]
    public class ChatBotController : Controller
    {
        private readonly OpenRouterService _chatService;

        public ChatBotController(OpenRouterService chatService)
        {
            _chatService = chatService;
        }

        [HttpPost("Ask")]
        public async Task<IActionResult> Ask([FromBody] ChatMessageRequest request)
        {
            if (string.IsNullOrWhiteSpace(request?.Message))
                return BadRequest(new { error = "Message must not be empty." });

            try
            {
                var reply = await _chatService.AskAsync(request.Message);
                return Ok(new { reply });
            }
            catch (HttpRequestException httpEx)
            {
                return StatusCode((int)HttpStatusCode.BadGateway, new { error = "Failed to connect to AI service", detail = httpEx.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Internal server error", detail = ex.Message });
            }
        }
    }

    public class ChatMessageRequest
    {
        public string Message { get; set; }
    }
}   