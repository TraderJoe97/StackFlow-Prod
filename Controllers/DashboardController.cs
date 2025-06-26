
using Microsoft.AspNetCore.Mvc;
using StackFlow.Data;
using System.Security.Claims; // Required for ClaimTypes

namespace StackFlow.Controllers
{
    public class DashboardController : Controller
    {
        private readonly AppDbContext _context;

        public DashboardController(AppDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {

            // Ensure the user is authenticated
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login", "Account"); // or your actual login controller
            }

            var role = User.FindFirstValue(ClaimTypes.Role);

            if (string.IsNullOrEmpty(role))
            {
                return Forbid(); // Role is not found or user has no assigned role
            }
            else
            {

                if (User.IsInRole("Admin"))
                    return RedirectToAction("Admin");
                if (User.IsInRole("ProjectLead"))
                    return RedirectToAction("ProjectLead");
                if (User.IsInRole("Developer"))
                    return RedirectToAction("Developer");
                if (User.IsInRole("Tester"))
                    return RedirectToAction("Tester");

                return Forbid();
            }

        }

        public IActionResult Developer()
        {
            return View();
        }

        public IActionResult Tester()
        {
            return View();
        }

        public IActionResult ProjectLead()
        {
            return View();
        }

        public IActionResult Admin()
        {
            return View();
        }
    }
}

