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

        // GET: /Chat
        [HttpGet("")]
        public IActionResult Chat()
        {
            return View(new Chat());
        }

        // POST: /Chat
        [HttpPost("")]
        public async Task<IActionResult> Chat(Chat model)
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

                // Add system context for recipe assistance
                if (chatHistory.Count == 0)
                {
                    chatHistory.AddSystemMessage("You are a helpful recipe and cooking assistant. You help users with cooking questions, recipe suggestions, meal planning, ingredient substitutions, cooking techniques, and food-related advice. Be friendly, informative, and practical in your responses.");
                }
                
                chatHistory.AddUserMessage(model.UserInput);
                var response = await _chatService.GetChatMessageContentAsync(chatHistory);

                model.AiResponse = response?.Content ?? "No response from AI.";
                model.History ??= new List<string>();
                model.History.Add("User: " + model.UserInput);
                model.History.Add("AI: " + model.AiResponse);
                
                // Clear the input for next message
                model.UserInput = "";
            }

            return View(model);
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
                try
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

                    // Add system context for recipe assistance
                    if (chatHistory.Count == 0)
                    {
                        chatHistory.AddSystemMessage("You are a helpful recipe and cooking assistant. You help users with cooking questions, recipe suggestions, meal planning, ingredient substitutions, cooking techniques, and food-related advice. Be friendly, informative, and practical in your responses.");
                    }

                    chatHistory.AddUserMessage(model.UserInput);
                    var response = await _chatService.GetChatMessageContentAsync(chatHistory);

                    model.AiResponse = response?.Content ?? "No response from AI.";
                    model.History ??= new List<string>();
                    model.History.Add("User: " + model.UserInput);
                    model.History.Add("AI: " + model.AiResponse);

                    return Json(new { 
                        success = true, 
                        aiResponse = model.AiResponse,
                        history = model.History 
                    });
                }
                catch (Exception ex)
                {
                    return Json(new { 
                        success = false, 
                        error = "Sorry, I encountered an error. Please try again.",
                        details = ex.Message 
                    });
                }
            }

            return Json(new { 
                success = false, 
                error = "Please enter a message." 
            });
        }
    }
}