using Microsoft.AspNetCore.Mvc;
using Service.Implementations;

namespace UI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AIServiceController : BaseAIController
    {
        public AIServiceController(ILogger<AIServiceController> logger, AIServiceFactory aiServiceFactory)
            : base(logger, aiServiceFactory) { }

        [HttpGet]
        [Route("SetUseOpenAI")]
        public IActionResult SetUseOpenAI([FromQuery] bool useOpenAI)
        {
            try
            {
                _logger.LogInformation("SetUseOpenAI method started.");
                HttpContext.Session.SetString("UseOpenAI", useOpenAI.ToString().ToLower());
                _logger.LogInformation($"Session value set: UseOpenAI = {HttpContext.Session.GetString("UseOpenAI")}");
                return Ok(new { message = $"AI service set to {(useOpenAI ? "OpenAI" : "Claude")}" });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in SetUseOpenAI: {ex}");
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An error occurred while setting the AI service." });
            }
        }

        [HttpGet]
        [Route("OpenSendMessage")]
        public IActionResult OpenSendMessage([FromQuery] bool useOpenAI)
        {
            try
            {
                _logger.LogInformation("OpenSendMessage method started.");
                HttpContext.Session.SetString("UseOpenAI", useOpenAI.ToString().ToLower());
                _logger.LogInformation($"Session value set: UseOpenAI = {HttpContext.Session.GetString("UseOpenAI")}");
                _logger.LogInformation("Redirecting to Home/SendMessage");
                return RedirectToAction("SendMessage", "Home");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in OpenSendMessage: {ex}");
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An error occurred while opening the SendMessage page." });
            }
        }
    }
}
