using Microsoft.AspNetCore.Mvc;
using Service.Implementations;

namespace UI.Controllers
{
    public abstract class BaseAIController : ControllerBase
    {
        protected readonly ILogger<BaseAIController> _logger;
        protected readonly AIServiceFactory _aiServiceFactory;

        protected BaseAIController(ILogger<BaseAIController> logger, AIServiceFactory aiServiceFactory)
        {
            _logger = logger;
            _aiServiceFactory = aiServiceFactory;
        }

        protected string GetOrCreateSessionId()
        {
            if (HttpContext.Session.GetString("ChatSessionId") == null)
            {
                HttpContext.Session.SetString("ChatSessionId", Guid.NewGuid().ToString());
            }
            return HttpContext.Session.GetString("ChatSessionId");
        }
    }
}
