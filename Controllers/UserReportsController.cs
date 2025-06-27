csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StackFlow.Models; // Add using statement for Models
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
            var users = _context.Users.Include(u => u.Tickets).ToList();
            return View(users);
        }
    }
}