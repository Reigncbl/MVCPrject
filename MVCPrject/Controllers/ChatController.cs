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

        // GET: /Chat/ChatbotPartial
        [HttpGet("ChatbotPartial")]
        public IActionResult ChatbotPartial()
        {
            return PartialView("_ChatbotPartial", new Chat());
        }

        // POST: /Chat/ChatbotPartial
        [HttpPost("ChatbotPartial")]
        public async Task<IActionResult> ChatbotPartial(Chat model)
        {
            if (ModelState.IsValid && !string.IsNullOrEmpty(model.UserInput))
            {
                var chatHistory = new ChatHistory();

                if (model.History != null)
                {
                    foreach (var line in model.History)
                    {
                        if (line.StartsWith("User: "))
                            chatHistory.AddUserMessage(line.Substring(6));
                        else if (line.StartsWith("AI: "))
                            chatHistory.AddAssistantMessage(line.Substring(4));
                    }
                }

                chatHistory.AddUserMessage(model.UserInput);
                var response = await _chatService.GetChatMessageContentAsync(chatHistory);

                model.AiResponse = response?.Content ?? "No response from AI.";
                model.History ??= new List<string>();
                model.History.Add("User: " + model.UserInput);
                model.History.Add("AI: " + model.AiResponse);
            }

            return PartialView("_ChatbotPartial", model);
        }
    }
}