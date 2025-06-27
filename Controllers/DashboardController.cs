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
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            // Fetch all projects. In a real application, you might filter this
            // based on user involvement (e.g., projects they created or are assigned to).
            var userId = int.Parse(userIdString);

            var allProjects = await _context.Project
                                            .Include(p => p.CreatedBy) // Include the user who created the project
                                            .ToListAsync();

            var Projects = allProjects.Where(p => p.Created_By == userId).ToList();

            return View(Projects);
        }

        public async Task<IActionResult> Admin()
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            // Fetch all projects. In a real application, you might filter this
            // based on user involvement (e.g., projects they created or are assigned to).
            var userId = int.Parse(userIdString);

            var allProjects = await _context.Project
                                            .Include(p => p.CreatedBy) // Include the user who created the project
                                            .ToListAsync();



            return View(allProjects);
        }

    }
}

