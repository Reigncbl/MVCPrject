using Microsoft.AspNetCore.Mvc;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace MVCPrject.Models
{
    public class AIResponse
    {
        private readonly Kernel _kernel;
        private readonly IChatCompletion _chatCompletionService;

        public AIResponse(Kernel kernel, IChatCompletion chatCompletionService)
        {
            _kernel = kernel;
            _chatCompletionService = chatCompletionService;
        }

        public async Task<string> GetResponse(string prompt)
        {
            // Initialize chat history
            var history = _chatCompletionService.CreateNewChat("assistant");
            history.AddUserMessage(prompt);

            // Get the response from the chat completion service
            var response = await _chatCompletionService.GetChatMessageAsync(
                history,
                kernel: _kernel
            );

            return response;
        }
    }
}
