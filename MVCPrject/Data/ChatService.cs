using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using System.Text.Json;

namespace MVCPrject.Data
{
    public interface IChatService
    {
        Task<string> GetResponseAsync(string userInput);
        List<(string Role, string Message)> GetChatHistory();
    }

    public class ChatService : IChatService
    {
        private const string SessionKey = "ChatHistory";
        private readonly IChatCompletionService _chat;
        private readonly IHttpContextAccessor _http;

        public ChatService(Kernel kernel, IHttpContextAccessor httpContextAccessor)
        {
            _chat = kernel.GetRequiredService<IChatCompletionService>();
            _http = httpContextAccessor;
        }

        public async Task<string> GetResponseAsync(string userInput)
        {
            var session = _http.HttpContext?.Session;
            var chatHistory = LoadChatHistory(session);

            chatHistory.AddUserMessage(userInput);

            var response = await _chat.GetChatMessageContentAsync(chatHistory);
            var aiResponse = response?.Content ?? "No response from AI.";

            chatHistory.AddAssistantMessage(aiResponse);
            SaveChatHistory(session, chatHistory);

            return aiResponse;
        }

        public List<(string Role, string Message)> GetChatHistory()
        {
            var session = _http.HttpContext?.Session;
            var history = LoadChatHistory(session);

            return history.Select(msg => (msg.Role.ToString(), msg.Content ?? string.Empty)).ToList();
        }

        private ChatHistory LoadChatHistory(ISession? session)
        {
            if (session == null) return new ChatHistory();

            try
            {
                var json = session.GetString(SessionKey);
                if (string.IsNullOrEmpty(json)) return new ChatHistory();

                var messages = JsonSerializer.Deserialize<List<(string Role, string Content)>>(json) ?? new();
                var history = new ChatHistory();

                foreach (var (role, content) in messages)
                {
                    if (role == "User") history.AddUserMessage(content);
                    else if (role == "Assistant") history.AddAssistantMessage(content);
                }

                return history;
            }
            catch
            {
                // Handle corrupted session data
                return new ChatHistory();
            }
        }

        private void SaveChatHistory(ISession? session, ChatHistory history)
        {
            if (session == null) return;

            var messages = history.Select(msg => (msg.Role.ToString(), msg.Content)).ToList();
            var json = JsonSerializer.Serialize(messages);

            session.SetString(SessionKey, json);
        }
    }
}
