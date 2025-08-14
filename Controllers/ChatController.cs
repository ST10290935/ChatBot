
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MentalHealthSupportApp.Models;

// NOTE: These are the correct namespaces for the Cohere.NET package.
using Cohere;
using Cohere.Types.Chat;
using ChatMessage = Cohere.Types.Chat.ChatMessage;

namespace MentalHealthSupportApp.Controllers
{
    public class ChatController : Controller
    {
        private readonly CohereClient _cohere;
        private readonly ILogger<ChatController> _logger;

        public ChatController(IConfiguration config, ILogger<ChatController> logger)
        {
            _logger = logger;

            // Support multiple ways to provide the key
            var apiKey =
                config["Cohere:ApiKey"] ??
                config["CohereApiKey"] ??
                Environment.GetEnvironmentVariable("COHERE_API_KEY");

            if (string.IsNullOrWhiteSpace(apiKey))
                throw new InvalidOperationException(
                    "Cohere API key not found. Set 'Cohere:ApiKey' in appsettings.json or COHERE_API_KEY env var.");

            _cohere = new CohereClient(apiKey);
        }

        [HttpGet]
        public IActionResult Index() => View();

        [HttpPost]
        public async Task<IActionResult> SendMessage([FromBody] UserMessage user)
        {
            if (user == null || string.IsNullOrWhiteSpace(user.Text))
                return BadRequest(new { error = "Message cannot be empty." });

            var userText = user.Text.Trim();

            try
            {
                // Build a small conversation for the Chat endpoint
                var messages = new List<ChatMessage>
                {
                    // Simple system-style instruction to keep responses supportive and safe
                    new ChatMessage { Role = "system", Content =
                        "You are a kind, empathetic mental health support assistant. " +
                        "Be supportive, non-judgmental, and encourage seeking professional help for crises. " +
                        "If a user expresses intent to harm themselves or others, advise contacting local emergency services immediately." },
                    new ChatMessage { Role = "user", Content = userText }
                };

                var response = await _cohere.ChatAsync(new ChatRequest
                {
                    Messages = messages,
                    MaxTokens = 200,      // Keep replies concise
                    Temperature = 0.7     // Balanced creativity
                    // Model can be omitted if the SDK defaults; add if available in your version:
                    // Model = "command-r-plus"
                });

                // Extract assistant text
                string aiText = "I'm here and listening.";
                var contentList = response?.Message?.Content as List<ChatResponseMessageText>;
                if (contentList is { Count: > 0 } && !string.IsNullOrWhiteSpace(contentList[0].Text))
                    aiText = contentList[0].Text.Trim();

                return Json(new { sender = "AI", text = aiText });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Cohere chat call failed.");
                // Graceful fallback so the UI still shows a reply
                return Json(new
                {
                    sender = "AI",
                    text = "Sorry — I couldn’t reach the support service right now, but I’m here to listen."
                });
            }
        }
    }
}
