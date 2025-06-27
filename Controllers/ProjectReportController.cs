using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StackFlow.Data;
using StackFlow.Models;
using System.Linq;
using System.Threading.Tasks;

namespace StackFlow.Controllers
{
    public class ProjectReportController : Controller
    {
        private readonly AppDbContext _context;

        public ProjectReportController(AppDbContext context)
        {
            _context = context;
        }

        // GET: /ProjectReport/Index
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var projects = await _context.Project
                                         .Include(p => p.Tickets)
                                         .OrderBy(p => p.Name)
                                         .ToListAsync();

            var projectReports = projects.Select(p => new ProjectReport
            {
                Project = p,
                TotalTickets = p.Tickets?.Count ?? 0,
                CompletedTickets = p.Tickets?.Count(t => t.Status == "Done") ?? 0,
                InProgressTickets = p.Tickets?.Count(t => t.Status == "In Progress") ?? 0,
                ToDoTickets = p.Tickets?.Count(t => t.Status == "To Do") ?? 0,
                InReviewTickets = p.Tickets?.Count(t => t.Status == "In Review") ?? 0
            }).ToList();

            return View(projectReports);
        }
    }
}
