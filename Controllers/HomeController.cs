using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using StackFlow.Models;
using System.Security.Claims; // Required for ClaimTypes


namespace StackFlow.Controllers
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
            // Ensure the user is authenticated
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Dashboard"); // or your actual login controller
            }

            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
