using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Service.Implementations;
using Service.Models;
using System.Text.RegularExpressions;

namespace UI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MessageController : BaseAIController
    {
        private readonly AIServiceFactory _aiServiceFactory;
        private BaseAIService _aiService;
        public MessageController(ILogger<MessageController> logger, AIServiceFactory aiServiceFactory)
            : base(logger, aiServiceFactory)
        {
            _aiServiceFactory = aiServiceFactory;
        }

        [HttpPost]
        [Route("SendMessage")]
        public async Task<IActionResult> SendMessage([FromBody] MessageRequest request)
        {
            _logger.LogInformation($"SendMessage endpoint hit. Request: {JsonConvert.SerializeObject(request)}");

            if (request == null || string.IsNullOrWhiteSpace(request.Message))
            {
                _logger.LogWarning("Invalid request payload");
                return BadRequest("Invalid request payload");
            }

            try
            {
                var sessionId = GetOrCreateSessionId();
                _logger.LogInformation($"Processing message for session {sessionId}: {request.Message}");

                string useOpenAIString = HttpContext.Session.GetString("UseOpenAI");
                _logger.LogInformation($"UseOpenAI session value: {useOpenAIString}");
                bool useOpenAI = !string.IsNullOrEmpty(useOpenAIString) && bool.Parse(useOpenAIString);

                _aiService = _aiServiceFactory.CreateAIService(useOpenAI);


                _logger.LogInformation($"Using AI service: {(useOpenAI ? "OpenAI" : "Claude")}");

                _aiService = _aiServiceFactory.CreateAIService(useOpenAI);

                string conversationHistory = HttpContext.Session.GetString("ConversationHistory") ?? "";
                var (aiResponse, missingDetail) = await _aiService.GetResponse(request.Message, conversationHistory, request.IncludeOrderInfo, request.UserDetails);

                // Process the AI response to extract any user details
                var updatedUserDetails = ExtractUserDetails(aiResponse, request.UserDetails);

                conversationHistory += $"User: {request.Message}\nBot: {aiResponse}\n";
                HttpContext.Session.SetString("ConversationHistory", conversationHistory);

                _logger.LogInformation($"Response for session {sessionId}: {aiResponse}");
                if (_aiService != null)
                {
                    await _aiService.SendUserDetailsToWebhookIfComplete(updatedUserDetails);
                }
                else
                {
                    _logger.LogError("AI service is null when attempting to send user details to Rebox");
                }
                var webhookStatus = "";
                if (_aiService != null)
                {
                    if (_aiService.AreUserDetailsComplete(updatedUserDetails))
                    {
                        await _aiService.SendUserDetailsToWebhookIfComplete(updatedUserDetails);
                        webhookStatus = updatedUserDetails.DetailsSentToWebhook ? "success" : "pending";
                    }
                }
                return Ok(new
                {
                    response = aiResponse,
                    userDetails = updatedUserDetails,
                    missingDetails = !string.IsNullOrEmpty(missingDetail),
                    reboxStatus= webhookStatus

                });
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogError($"Unauthorized access to API: {ex}");
                return StatusCode(StatusCodes.Status401Unauthorized, new { message = "מצטערים, יש בעיה בגישה למערכת. צוות הפיתוח יטפל בבעיה בהקדם." });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in SendMessage: {ex}");
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = $"מצטערים, אירעה שגיאה בעת עיבוד הבקשה שלך: {ex.Message}" });
            }
        }

        private UserDetails ExtractUserDetails(string aiResponse, UserDetails currentDetails)
        {
            // Example patterns to look for in the AI response
            var patterns = new Dictionary<string, string>
            {
                { @"שם פרטי[:]?\s*(\w+)", "FirstName" },
                { @"שם משפחה[:]?\s*(\w+)", "LastName" },
                { @"אימייל[:]?\s*(\S+@\S+\.\S+)", "Email" },
                { @"טלפון[:]?\s*(\d+)", "Phone" }
            };

            foreach (var pattern in patterns)
            {
                var match = Regex.Match(aiResponse, pattern.Key, RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    var propertyInfo = typeof(UserDetails).GetProperty(pattern.Value);
                    if (propertyInfo != null)
                    {
                        propertyInfo.SetValue(currentDetails, match.Groups[1].Value);
                    }
                }
            }

            return currentDetails;
        }

        [HttpPost]
        [Route("ClearConversationHistory")]
        public IActionResult ClearConversationHistory()
        {
            try
            {
                HttpContext.Session.Remove("ConversationHistory");
                _logger.LogInformation("Conversation history cleared successfully");
                return Ok(new { message = "Conversation history cleared successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error clearing conversation history: {ex}");
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An error occurred while clearing conversation history." });
            }
        }
    }
}