using Microsoft.AspNetCore.Mvc;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using MVCPrject.Models;

namespace MVCPrject.Controllers
{
    [Route("Chat")]
    public class ChatController : Controller
    {
        private readonly IChatCompletionService _chatService;

        public ChatController(Kernel kernel)
        {
            _chatService = kernel.GetRequiredService<IChatCompletionService>();
        }

        [HttpGet("")]
        [HttpGet("Chat")]
        public IActionResult Chat()
        {
            return View(new Chat());
        }

        [HttpPost("")]
        [HttpPost("Chat")]
        public async Task<IActionResult> Chat(Chat model)
        {
            if (ModelState.IsValid && !string.IsNullOrEmpty(model.UserInput))
            {
                var chatHistory = new ChatHistory();
                chatHistory.AddUserMessage(model.UserInput);

                var response = await _chatService.GetChatMessageContentAsync(chatHistory);
                model.AiResponse = response?.Content ?? "No response from AI.";
            }

            return View(model);
        }
    }
}
