using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using StackFlow.Data;
using StackFlow.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace StackFlow.Controllers
{
    public class TaskController : Controller
    {
        private readonly AppDbContext _context;

        public TaskController(AppDbContext context)
        {
            _context = context;
        }

        // GET: /Task
        public async Task<IActionResult> Index()
        {
            var tickets = await _context.Ticket
                .Include(t => t.Project)
                .Include(t => t.AssignedTo)
                .Include(t => t.CreatedBy)
                .ToListAsync();

            return View("~/Views/Ticket/Index.cshtml", tickets);
        }

        // GET: /Task/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var ticket = await _context.Ticket
                .Include(t => t.Project)
                .Include(t => t.AssignedTo)
                .Include(t => t.CreatedBy)
                .FirstOrDefaultAsync(m => m.Id == id);

            return ticket == null ? NotFound() : View("~/Views/Ticket/Details.cshtml", ticket);
        }

        // GET: /Task/Create
        public IActionResult Create()
        {
            PopulateDropdowns();
            return View("~/Views/Ticket/Create.cshtml");
        }

        // POST: /Task/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Title,Description,Due_Date,Priority,Status,Project_Id,Assigned_To")] Ticket ticket)
        {
            if (ModelState.IsValid)
            {
                ticket.Created_At = DateTime.Now;
                _context.Add(ticket);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            PopulateDropdowns(ticket);
            return View("~/Views/Ticket/Create.cshtml", ticket);
        }

        // GET: /Task/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var ticket = await _context.Ticket.FindAsync(id);
            if (ticket == null) return NotFound();

            PopulateDropdowns(ticket);
            return View("~/Views/Ticket/Update.cshtml", ticket);
        }

        // POST: /Task/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Title,Description,Due_Date,Priority,Status,Project_Id,Assigned_To,Created_By,Created_At,Completed_At")] Ticket ticket)
        {
            if (id != ticket.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(ticket);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!TicketExists(ticket.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }

            PopulateDropdowns(ticket);
            return View("~/Views/Ticket/Update.cshtml", ticket);
        }

        // GET: /Task/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var ticket = await _context.Ticket
                .Include(t => t.Project)
                .FirstOrDefaultAsync(m => m.Id == id);

            return ticket == null ? NotFound() : View("~/Views/Ticket/Delete.cshtml", ticket);
        }

        // POST: /Task/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var ticket = await _context.Ticket.FindAsync(id);
            if (ticket != null)
            {
                _context.Ticket.Remove(ticket);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        // Util to repopulate dropdowns
        private void PopulateDropdowns(Ticket ticket = null)
        {
            ViewBag.AssignedToList = new SelectList(_context.User, "Id", "Name", ticket?.Assigned_To);
            ViewBag.ProjectList = new SelectList(_context.Project, "Id", "Name", ticket?.Project_Id);
        }

        private bool TicketExists(int id)
        {
            return _context.Ticket.Any(e => e.Id == id);
        }
    }
}
