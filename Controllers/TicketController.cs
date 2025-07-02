using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using StackFlow.Data;
using StackFlow.Hubs;
using StackFlow.Models;
using StackFlow.Utils;
using StackFlow.ViewModels; // Ensure this is present
using System.Security.Claims;

namespace StackFlow.Controllers
{
    [Authorize] // All actions in this controller require authentication
    public class TicketController : Controller // Reverted from TaskController, if it was named that
    {
        private readonly AppDbContext _context;
        private readonly IHubContext<DashboardHub> _hubContext;

        public TicketController(AppDbContext context, IHubContext<DashboardHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }

        /// <summary>
        /// Displays a list of all tickets.
        /// Accessible to all authenticated users.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Index(int? pageNumber, int? pageSize)
        {
            int currentPage = pageNumber ?? 1; // If no pageNumber is provided, default to 1
            int itemsPerPage = pageSize ?? 5; // If no pageSize is provided, default to 10

            var tickets =  _context.Ticket
                                        .Include(t => t.Project)
                                        .Include(t => t.AssignedTo)
                                        .Include(t => t.CreatedBy)
                                        .OrderByDescending(t => t.Created_At) // Order by creation date for logical display
                                        .AsNoTracking();

            // Create the paginated list
            var paginatedTickets = await PaginatedList<Ticket>.CreateAsync(
                                        tickets, currentPage, itemsPerPage);

            return View(paginatedTickets);

        }

        /// <summary>
        /// Displays the form for creating a new ticket.
        /// Accessible only to Admin and Project Managers.
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Admin,Project Manager")]
        public async Task<IActionResult> CreateTicket() // Reverted from CreateTask
        {
            ViewBag.Projects = new SelectList(await _context.Project.ToListAsync(), "Id", "Name");
            ViewBag.Users = new SelectList(await _context.User.ToListAsync(), "Id", "Name");
            ViewBag.TicketStatuses = new SelectList(new List<string> { "To_Do", "In_Progress", "In_Review", "Done" }); // Reverted from TaskStatuses
            ViewBag.TicketPriorities = new SelectList(new List<string> { "Low", "Medium", "High" }); // Reverted from TaskPriorities
            return View(new Ticket()); // Reverted from Task
        }

        /// <summary>
        /// Handles the submission of the new ticket creation form.
        /// Accessible only to Admin and Project Managers.
        /// </summary>
        /// <param name="ticket">The Ticket model populated from the form.</param>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Project Manager")]
        public async Task<IActionResult> CreateTicket([Bind("Title,Description,Project_Id,Assigned_To,Status,Priority,Due_Date")] Ticket ticket)
        {
            // Remove navigation properties from ModelState validation, as they are not posted from the form
            ModelState.Remove("Project");
            ModelState.Remove("AssignedTo");
            ModelState.Remove("Comments"); // Reverted from TaskComments
            ModelState.Remove("CreatedBy"); // Reverted from TaskCreatedBy
            ModelState.Remove("CreatedByUserId"); // Reverted from TaskCreatedByUserId
            ModelState.Remove("CreatedAt"); // Reverted from TaskCreatedAt
            ModelState.Remove("CompletedAt"); // Reverted from TaskCompletedAt


            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdString, out int currentUserId))
            {
                TempData["ErrorMessage"] = "Authentication error: Could not identify user for ticket creation.";
                return RedirectToAction("Login", "Account");
            }

            ticket.Created_By = currentUserId;
            ticket.Created_At = DateTime.UtcNow;

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Add(ticket);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = $"Ticket '{ticket.Title}' created successfully!";

                    // Send minimal data: action and ticket ID
                    await _hubContext.Clients.All.SendAsync("ReceiveTicketUpdate", "created", ticket.Id);

                    return RedirectToAction("Index", "Dashboard"); // Redirect to Dashboard Index
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = $"Error creating ticket: {ex.Message}";
                    ViewBag.Projects = new SelectList(await _context.Project.ToListAsync(), "Id", "Name", ticket.Project_Id);
                    ViewBag.Users = new SelectList(await _context.User.ToListAsync(), "Id", "Name", ticket.Assigned_To);
                    ViewBag.TicketStatuses = new SelectList(new List<string> { "To_Do", "In_Progress", "In_Review", "Done" }, ticket.Status); // Reverted
                    ViewBag.TicketPriorities = new SelectList(new List<string> { "Low", "Medium", "High" }, ticket.Priority); // Reverted
                    return View(ticket);
                }
            }

            TempData["ErrorMessage"] = "Please correct the errors in the form.";
            ViewBag.Projects = new SelectList(await _context.Project.ToListAsync(), "Id", "Name", ticket.Project_Id);
            ViewBag.Users = new SelectList(await _context.User.ToListAsync(), "Id", "Name", ticket.Assigned_To);
            ViewBag.TicketStatuses = new SelectList(new List<string> { "To_Do", "In_Progress", "In_Review", "Done" }, ticket.Status); // Reverted
            ViewBag.TicketPriorities = new SelectList(new List<string> { "Low", "Medium", "High" }, ticket.Priority); // Reverted

            foreach (var modelStateEntry in ModelState.Values)
            {
                foreach (var error in modelStateEntry.Errors)
                {
                    Console.WriteLine($"Validation Error (CreateTicket): {error.ErrorMessage}");
                }
            }
            return View(ticket);
        }

        /// <summary>
        /// Displays the detailed view of a single ticket.
        /// Accessible to all authenticated users.
        /// </summary>
        /// <param name="id">The ID of the ticket to view.</param>
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> TicketDetails(int id) // Reverted from TaskDetails
        {
            var ticket = await _context.Ticket // Reverted from Tasks
                                     .Include(t => t.Project)
                                     .Include(t => t.AssignedTo)
                                     .Include(t => t.CreatedBy) // Reverted from TaskCreatedBy
                                     .Include(t => t.TicketComments) // Reverted from TaskComments
                                        .ThenInclude(tc => tc.CreatedBy)
                                     .FirstOrDefaultAsync(m => m.Id == id);

            if (ticket == null)
            {
                return NotFound();
            }

            var ticketViewModel = new TicketViewModel // Reverted from TaskViewModel
            {
                Id = ticket.Id,
                Title = ticket.Title, 
                Description = ticket.Description, 
                ProjectId = ticket.Project_Id,
                Project = ticket.Project,
                AssignedToUserId = ticket.Assigned_To,
                AssignedTo = ticket.AssignedTo,
                Status = ticket.Status, // Reverted
                Priority = ticket.Priority, // Reverted
                CreatedByUserId = ticket.Created_By,
                CreatedBy = ticket.CreatedBy, // Reverted
                CreatedAt = ticket.Created_At, // Reverted
                DueDate = ticket.Due_Date, // Reverted
                CompletedAt = ticket.Completed_At, // Reverted
                Comments = ticket.TicketComments.OrderBy(c => c.Created_At).ToList(), // Reverted
                AssignedToUsername = ticket.AssignedTo?.Name,
                ProjectName = ticket.Project?.Name,
                CreatedByUsername = ticket.CreatedBy?.Name // Reverted
            };

            // Force a default selected value for the dropdown if Model.Status is null or empty
            var currentStatus = string.IsNullOrEmpty(ticket.Status) ? "To Do" : ticket.Status;

            // Create a list of SelectListItem explicitly to ensure values are set
            var statusOptions = new List<SelectListItem>
            {
                new SelectListItem { Value = "To_Do", Text = "To Do", Selected = (currentStatus == "To Do") },
                new SelectListItem { Value = "In_Progress", Text = "In Progress", Selected = (currentStatus == "In Progress") },
                new SelectListItem { Value = "In_Review", Text = "In Review", Selected = (currentStatus == "In Review") },
                new SelectListItem { Value = "Done", Text = "Done", Selected = (currentStatus == "Done") }
            };
            ViewBag.TicketStatuses = new SelectList(statusOptions, "Value", "Text", currentStatus);


            return View(ticketViewModel);
        }

        /// <summary>
        /// Handles the submission for updating a ticket's status from the Ticket Details page.
        /// Accessible to all authenticated users.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AllowAnonymous]
        public async Task<IActionResult> UpdateTicketStatus(int ticketId, string newStatus) // Reverted from UpdateTaskStatus
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdString, out int currentUserId))
            {
                TempData["ErrorMessage"] = "Authentication error: Could not identify user for status update.";
                return Unauthorized();
            }

            var ticket = await _context.Ticket.FindAsync(ticketId); // Reverted from Tasks.FindAsync
            if (ticket == null)
            {
                return NotFound();
            }

            // Trim whitespace from newStatus for robust comparison
            newStatus = newStatus?.Trim();

            var allowedStatuses = new List<string> { "To_Do", "In_Progress", "In_Review", "Done" };
            if (!allowedStatuses.Contains(newStatus))
            {
                TempData["ErrorMessage"] = $"Invalid ticket status provided: '{newStatus}'. Expected one of: {string.Join(", ", allowedStatuses)}.";
                return RedirectToAction(nameof(TicketDetails), new { id = ticketId });
            }

            string oldStatus = ticket.Status; // Reverted
            ticket.Status = newStatus; // Reverted
            if (oldStatus != newStatus)
            {
                if (newStatus == "Done") // Reverted
                {
                    ticket.Completed_At = DateTime.UtcNow; // Reverted
                }
            }

            try
            {
                _context.Update(ticket);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Ticket '{ticket.Title}' status updated to '{newStatus}' successfully!";

                // Send minimal data: action and ticket ID
                await _hubContext.Clients.All.SendAsync("ReceiveTicketUpdate", "updated", ticket.Id, oldStatus);

            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error updating ticket status: {ex.Message}";
            }

            return RedirectToAction(nameof(TicketDetails), new { id = ticketId });
        }


        /// <summary>
        /// Handles the submission for adding a new comment to a ticket.
        /// Accessible to all authenticated users.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AllowAnonymous]
        public async Task<IActionResult> AddComment(int ticketId, string commentText) // Reverted from AddComment
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdString, out int currentUserId))
            {
                return Unauthorized();
            }

            if (string.IsNullOrWhiteSpace(commentText))
            {
                ModelState.AddModelError("commentText", "Comment cannot be empty.");
                TempData["ErrorMessage"] = "Comment cannot be empty.";
                return RedirectToAction(nameof(TicketDetails), new { id = ticketId });
            }

            var ticket = await _context.Ticket.FirstOrDefaultAsync(t => t.Id == ticketId); // Reverted from Tasks.FirstOrDefaultAsync
            if (ticket == null)
            {
                TempData["ErrorMessage"] = "Ticket not found.";
                return RedirectToAction("Index", "Dashboard"); // Redirect to Dashboard Index
            }

            var ticketComment = new Comment // Reverted from TaskComment
            {
                Ticket_Id = ticketId, // Reverted
                Created_By = currentUserId,
                Content = commentText.Trim(),
                Created_At = DateTime.UtcNow
            };

            _context.TicketComment.Add(ticketComment); // Reverted
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Comment added successfully!";
            // Comments are not being updated in the main dashboard view, so a simple notification is fine.
            await _hubContext.Clients.All.SendAsync("ReceiveTicketUpdate", "commented", ticket.Id);

            return RedirectToAction(nameof(TicketDetails), new { id = ticketId });
        }

        /// <summary>
        /// Displays the form for editing an existing ticket.
        /// Accessible only to Admin and Project Managers.
        /// </summary>
        /// <param name="id">The ID of the ticket to edit.</param>
        [HttpGet]
        [Authorize(Roles = "Admin,Project Manager")]
        public async Task<IActionResult> EditTicket(int id) // Reverted from EditTask
        {
            var ticket = await _context.Ticket.FindAsync(id); // Reverted from Tasks.FindAsync
            if (ticket == null)
            {
                return NotFound();
            }

            ViewBag.Projects = new SelectList(await _context.Project.ToListAsync(), "Id", "Name", ticket.Project_Id);
            ViewBag.Users = new SelectList(await _context.User.ToListAsync(), "Id", "Name");
            ViewBag.TicketStatuses = new SelectList(new List<string> { "To_Do", "In_Progress", "In_Review", "Done" }, ticket.Status); // Reverted
            ViewBag.TicketPriorities = new SelectList(new List<string> { "Low", "Medium", "High" }, ticket.Priority); // Reverted

            return View(ticket);
        }

        /// <summary>
        /// Handles the submission of the edited ticket form.
        /// Accessible only to Admin and Project Managers.
        /// </summary>
        /// <param name="id">The ID of the ticket being edited.</param>
        /// <param name="ticket">The Ticket model populated from the form.</param>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Project Manager")]
        public async Task<IActionResult> EditTicket(int id, [Bind("Id,Title,Description,Project_Id,Assigned_To,Status,Priority,Due_Date,Completed_At")] Ticket ticket) // Reverted model binding properties
        {
            if (id != ticket.Id)
            {
                return NotFound();
            }

            // Remove navigation properties from ModelState validation, as they are not posted from the form
            ModelState.Remove("Project");
            ModelState.Remove("AssignedTo");
            ModelState.Remove("Comments"); // Reverted
            ModelState.Remove("CreatedBy"); // Reverted
            ModelState.Remove("CreatedByUserId"); // Reverted
            ModelState.Remove("CreatedAt"); // Reverted


            if (ModelState.IsValid)
            {
                try
                {
                    // Fetch the original ticket to preserve CreatedByUserId and CreatedAt
                    var originalTicket = await _context.Ticket.AsNoTracking().FirstOrDefaultAsync(t => t.Id == id); // Reverted
                    if (originalTicket == null)
                    {
                        return NotFound();
                    }
                    ticket.Created_By = originalTicket.Created_By;
                    ticket.Created_At = originalTicket.Created_At;


                    _context.Update(ticket);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = $"Ticket '{ticket.Title}' updated successfully!";

                    // Send minimal data: action and ticket ID
                    await _hubContext.Clients.All.SendAsync("ReceiveTicketUpdate", "updated", ticket.Id, originalTicket.Status);

                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await _context.Ticket.AnyAsync(e => e.Id == ticket.Id)) // Reverted
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = $"Error updating ticket: {ex.Message}";
                    ViewBag.Projects = new SelectList(await _context.Project.ToListAsync(), "Id", "Name", ticket.Project_Id);
                    ViewBag.Users = new SelectList(await _context.User.ToListAsync(), "Id", "Name", ticket.Assigned_To);
                    ViewBag.TicketStatuses = new SelectList(new List<string> { "To_Do", "In_Progress", "In_Review", "Done" }, ticket.Status); // Reverted
                    ViewBag.TicketPriorities = new SelectList(new List<string> { "Low", "Medium", "High" }, ticket.Priority); // Reverted
                    return View(ticket);
                }
                return RedirectToAction("Index", "Dashboard"); // Redirect to Dashboard Index
            }

            TempData["ErrorMessage"] = "Please correct the errors in the form.";
            ViewBag.Projects = new SelectList(await _context.Project.ToListAsync(), "Id", "Name", ticket.Project_Id);
            ViewBag.Users = new SelectList(await _context.User.ToListAsync(), "Id", "Name", ticket.Assigned_To);
            ViewBag.TicketStatuses = new SelectList(new List<string> { "To_Do", "In_Progress", "In_Review", "Done" }, ticket.Status); // Reverted
            ViewBag.TicketPriorities = new SelectList(new List<string> { "Low", "Medium", "High" }, ticket.Priority); // Reverted

            foreach (var modelStateEntry in ModelState.Values)
            {
                foreach (var error in modelStateEntry.Errors)
                {
                    Console.WriteLine($"Validation Error (EditTicket): {error.ErrorMessage}");
                }
            }
            return View(ticket);
        }

        /// <summary>
        /// POST action to delete a ticket.
        /// Accessible only to Admin and Project Managers.
        /// </summary>
        /// <param name="id">The ID of the ticket to delete.</param>
        [HttpPost, ActionName("DeleteTicket")] // Reverted from DeleteTask
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Project Manager")]
        public async Task<IActionResult> DeleteTicketConfirmed(int id) // Reverted from DeleteTaskConfirmed
        {
            var ticket = await _context.Ticket.FindAsync(id); // Reverted from Tasks.FindAsync
            if (ticket == null)
            {
                TempData["ErrorMessage"] = "Ticket not found.";
                return NotFound();
            }

            string ticketTitle = ticket.Title; // Reverted
            string oldStatus = ticket.Status; // Reverted

            try
            {
                _context.Ticket.Remove(ticket); // Reverted
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Ticket '{ticketTitle}' deleted successfully.";
                // Send minimal data: action and ticket ID
                await _hubContext.Clients.All.SendAsync("ReceiveTicketUpdate", "deleted", id, oldStatus);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error deleting ticket: {ex.Message}";
            }
            return RedirectToAction("Index", "Dashboard"); // Redirect to Dashboard Index
        }
    }
}
