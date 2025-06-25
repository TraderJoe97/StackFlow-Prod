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
        // Displays a list of all tickets with related Project, Assigned User, and Creator
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
        // Displays the details of a specific ticket based on ID
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
        // Displays the ticket creation form
        public IActionResult Create()
        {
            PopulateDropdowns(); // Load dropdown lists for Project and AssignedTo
            return View("~/Views/Ticket/Create.cshtml");
        }

        // POST: /Task/Create
        // Handles ticket form submission for creating a new ticket
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Title,Description,Due_Date,Priority,Status,Project_Id,Assigned_To")] Ticket ticket)
        {
            if (ModelState.IsValid)
            {
                ticket.Created_At = DateTime.Now; // Set creation date
                _context.Add(ticket);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            // If validation fails, reload dropdowns and return view with model
            PopulateDropdowns(ticket);
            return View("~/Views/Ticket/Create.cshtml", ticket);
        }

        // GET: /Task/Edit/5
        // Displays the edit form for a ticket
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var ticket = await _context.Ticket.FindAsync(id);
            if (ticket == null) return NotFound();

            PopulateDropdowns(ticket);
            return View("~/Views/Ticket/Update.cshtml", ticket);
        }

        // POST: /Task/Edit/5
        // Handles form submission for editing an existing ticket
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

            // If validation fails, reload dropdowns and return view with model
            PopulateDropdowns(ticket);
            return View("~/Views/Ticket/Update.cshtml", ticket);
        }

        // GET: /Task/Delete/5
        // Displays confirmation page for deleting a ticket
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var ticket = await _context.Ticket
                .Include(t => t.Project)
                .FirstOrDefaultAsync(m => m.Id == id);

            return ticket == null ? NotFound() : View("~/Views/Ticket/Delete.cshtml", ticket);
        }

        // POST: /Task/Delete/5
        // Finalizes the ticket deletion
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

        // Helper method to populate dropdown lists for forms
        private void PopulateDropdowns(Ticket ticket = null)
        {
            ViewBag.AssignedToList = new SelectList(_context.User, "Id", "Name", ticket?.Assigned_To);
            ViewBag.ProjectList = new SelectList(_context.Project, "Id", "Name", ticket?.Project_Id);
        }

        // Checks if a ticket exists based on ID
        private bool TicketExists(int id)
        {
            return _context.Ticket.Any(e => e.Id == id);
        }
    }
}
