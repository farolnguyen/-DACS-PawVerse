using Microsoft.AspNetCore.Mvc;
using PawVerse.Services.Interfaces;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace PawVerse.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    public class ChatbotController : ControllerBase
    {
        private readonly IChatbotService _chatbotService;
        private readonly ILogger<ChatbotController> _logger;

        public ChatbotController(IChatbotService chatbotService, ILogger<ChatbotController> logger)
        {
            _chatbotService = chatbotService;
            _logger = logger;
        }

        [HttpPost("send-message")]
        public async Task<IActionResult> SendMessage([FromBody] ChatMessageRequest request)
        {
            if (string.IsNullOrEmpty(request.Message))
            {
                return BadRequest("Message cannot be empty");
            }

            try
            {
                var response = await _chatbotService.SendMessageAsync(request.Message);
                return Ok(new { response = response });
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error sending message to chatbot");
                return StatusCode(500, "An error occurred while communicating with the chatbot");
            }
        }
    }

    public class ChatMessageRequest
    {
        public string Message { get; set; }
    }

    public class ContinueConversationRequest
    {
        public string ConversationId { get; set; }
        public string Message { get; set; }
    }
}
