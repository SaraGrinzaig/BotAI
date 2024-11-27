using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using UI.Models;

namespace UI.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            string useOpenAI = HttpContext.Session.GetString("UseOpenAI");
            if (useOpenAI != null)
            {
                ViewBag.UseOpenAI = bool.Parse(useOpenAI);
                _logger.LogInformation($"HomeController: UseOpenAI set to {useOpenAI}");
            }
            else
            {
                _logger.LogWarning("HomeController: UseOpenAI is not set in session.");
                ViewBag.UseOpenAI = (bool?)null; // Explicitly set to null
            }
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        public IActionResult SendMessage()
        {
            string useOpenAIString = HttpContext.Session.GetString("UseOpenAI");
            bool useOpenAI = !string.IsNullOrEmpty(useOpenAIString) && bool.Parse(useOpenAIString);
            _logger.LogInformation($"HomeController: UseOpenAI set to {useOpenAI} on SendMessage");

            var viewModel = new AIServiceViewModel
            {
                UseOpenAI = useOpenAI
            };
            return View(viewModel);
        }




        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        public IActionResult LandingPage()
        {
            return View();
        }

    }
}
