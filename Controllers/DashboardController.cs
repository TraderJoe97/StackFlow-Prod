using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StackFlow.Data;
using System.Security.Claims;

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

        public async Task<IActionResult> Developer()
        {
            // Get the current user's ID from claims
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var allTasks = await _context.Ticket
                             .Include(t => t.Project)
                             .Include(t => t.AssignedTo)
                             .ToListAsync();

            var userId = int.Parse(userIdString);
            var AssignedToMeTasks = allTasks.Where(t => t.Assigned_To == userId).ToList();
            return View(AssignedToMeTasks);

        }


        public async Task<ActionResult> Tester()
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var allTasks = await _context.Ticket
                             .Include(t => t.Project)
                             .Include(t => t.AssignedTo)
                             .ToListAsync();

            var userId = int.Parse(userIdString);
            var AssignedToMeTasks = allTasks.Where(t => t.Assigned_To == userId).ToList();
            return View(AssignedToMeTasks);

        }

        public async  Task<IActionResult> ProjectLead()
        {


            return View();
        }

        public async Task<IActionResult> Admin()
        {


            return View();
        }

    }
}

