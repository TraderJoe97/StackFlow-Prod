
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StackFlow.Models;
using System.Linq;
using Microsoft.AspNetCore.Mvc.Rendering;
using StackFlow.Data;


namespace StackFlow.Controllers
{
    [Authorize(Roles = "Admin")]
    public class UserReportsController : Controller
    {
        private readonly AppDbContext _context;

        public UserReportsController(AppDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var users = _context.Users
                .Include(u => u.Tickets)
                .Include(u => u.Role)
                .ToList();

            var userReports = users.Select(user => new UserReportViewModel
            {
                User = user,
                TotalTicketsAssigned = user.Tickets.Count,
                ToDoTicketsAssigned = user.Tickets.Count(t => t.Status == "To Do"),
                InProgressTicketsAssigned = user.Tickets.Count(t => t.Status == "In Progress"),
                InReviewTicketsAssigned = user.Tickets.Count(t => t.Status == "In Review"),
                CompletedTicketsAssigned = user.Tickets.Count(t => t.Status == "Completed")
            }).ToList();

            ViewBag.CurrentUserId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value);
            ViewBag.Roles = new SelectList(_context.Roles, "Id", "Title");

            return View(userReports);
        }

        [HttpPost]
        public IActionResult DeleteUser(int id)
        {
            var userToDelete = _context.Users.Find(id);
            if (userToDelete == null)
            {
                TempData["ErrorMessage"] = "User not found.";
                return RedirectToAction(nameof(Index));
            }

            userToDelete.IsActive = false;
            _context.SaveChanges();

            TempData["SuccessMessage"] = "User deleted successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public IActionResult UpdateUserRole(int userId, int newRoleId)
        {
            var userToUpdate = _context.Users.Find(userId);
            if (userToUpdate == null)
            {
                TempData["ErrorMessage"] = "User not found.";
                return RedirectToAction(nameof(Index));
            }

            userToUpdate.RoleId = newRoleId;
            _context.SaveChanges();

            TempData["SuccessMessage"] = $"{userToUpdate.Username}'s role updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        // Define UserReportViewModel here for simplicity
        public class UserReportViewModel
        {
            public Models.User User { get; set; }
            public int TotalTicketsAssigned { get; set; }
            public int ToDoTicketsAssigned { get; set; }
            public int InProgressTicketsAssigned { get; set; }
            public int InReviewTicketsAssigned { get; set; }
            public int CompletedTicketsAssigned { get; set; }
        }
    }
}