using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using StackFlow.Data;
using StackFlow.Models;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace StackFlow.Controllers
{
    [Authorize]
    [Route("Ticket")] 
    public class TicketController : Controller
    {
        private readonly AppDbContext _context;

        public TicketController(AppDbContext context)
        {
            _context = context;
        }

        // GET: /Ticket
        // Displays a list of all tickets with related Project, Assigned User, and Creator
        [HttpGet("")]
        public async Task<IActionResult> Index()
        {
            var tickets = await _context.Ticket
                .Include(t => t.Project)
                .Include(t => t.AssignedTo)
                .Include(t => t.CreatedBy)
                .ToListAsync();

            return View("~/Views/Ticket/Index.cshtml", tickets);
        }

        // GET: /Ticket/Details/5
        // Displays the details of a specific ticket based on ID
        [HttpGet("Details/{id}")]

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

        // GET: /Ticket/Create
        // Displays the ticket creation form
        [HttpGet("Create")]
        public IActionResult Create()
        {
            PopulateDropdowns(); // Load dropdown lists for Project and AssignedTo
            return View("~/Views/Ticket/Create.cshtml");
        }

        // POST: /Ticket/Create
        // Handles ticket form submission for creating a new ticket
        [HttpPost("Create")]
        [Authorize(Roles = "Admin,Project Manager")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Title,Description,Due_Date,Priority,Status,Project_Id,Assigned_To")] Ticket ticket)
        {
            if (ModelState.IsValid)
            {
                ticket.Created_At = DateTime.Now;

                var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (int.TryParse(userIdString, out int userId))
                {
                    ticket.Created_By = userId;
                }
                else
                {
                    // If user not found, return with error
                    ModelState.AddModelError("", "Could not determine the creator of the ticket. Please log in.");
                    PopulateDropdowns(ticket);
                    return View("~/Views/Ticket/Create.cshtml", ticket);
                }

                _context.Add(ticket);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }

            PopulateDropdowns(ticket);
            return View("~/Views/Ticket/Create.cshtml", ticket);
        }

        // GET: /Ticket/Edit/5
        // Displays the edit form for a ticket
        [HttpGet("Edit/{id}")]
        [Authorize(Roles = "Admin,Project Manager")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var ticket = await _context.Ticket.FindAsync(id);
            if (ticket == null) return NotFound();

            PopulateDropdowns(ticket);
            return View("~/Views/Ticket/Update.cshtml", ticket);
        }

        // POST: /Ticket/Edit/5
        // Handles form submission for editing an existing ticket
        [HttpPost("Edit/{id}")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Project Manager")]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Title,Description,Due_Date,Priority,Status,Project_Id,Assigned_To")] Ticket updatedTicket)
        {
            // Make sure the ID from the route matches the form
            if (id != updatedTicket.Id) return NotFound();

            // Load the existing ticket from DB (includes Created_By)
            var existingTicket = await _context.Ticket.FindAsync(id);
            if (existingTicket == null) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    // Only update fields that can be changed
                    existingTicket.Title = updatedTicket.Title;
                    existingTicket.Description = updatedTicket.Description;
                    existingTicket.Due_Date = updatedTicket.Due_Date;
                    existingTicket.Priority = updatedTicket.Priority;
                    existingTicket.Status = updatedTicket.Status;
                    existingTicket.Project_Id = updatedTicket.Project_Id;
                    existingTicket.Assigned_To = updatedTicket.Assigned_To;

                    // Save changes without altering Created_By or Created_At
                    _context.Update(existingTicket);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!TicketExists(updatedTicket.Id)) return NotFound();
                    throw;
                }

                return RedirectToAction(nameof(Index));
            }

            // If validation fails, reload dropdowns and return view with model
            PopulateDropdowns(updatedTicket);
            return View("~/Views/Ticket/Update.cshtml", updatedTicket);
        }

        // GET: /Ticket/Delete/5
        // Displays confirmation page for deleting a ticket
        [HttpGet("Delete/{id}")]
        [Authorize(Roles = "Admin,Project Manager")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var ticket = await _context.Ticket
                .Include(t => t.Project)
                .FirstOrDefaultAsync(m => m.Id == id);

            return ticket == null ? NotFound() : View("~/Views/Ticket/Delete.cshtml", ticket);
        }

        // POST: /Ticket/Delete/5
        // Finalizes the ticket deletion
        [HttpPost("Delete/{id}"), ActionName("DeleteConfirmed")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Project Manager")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var ticket = await _context.Ticket.FindAsync(id);
            if (ticket == null) return NotFound();

            _context.Ticket.Remove(ticket);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // Helper method to populate dropdown lists for forms
        private void PopulateDropdowns(Ticket? ticket = null)
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
