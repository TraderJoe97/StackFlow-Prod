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
using StackFlow.Services; // Added for IEmailService
using System.Collections.Generic; // Added for Dictionary

namespace StackFlow.Controllers
{
    [Authorize] // All actions in this controller require authentication
    public class TicketController : Controller // Reverted from TaskController, if it was named that
    {
        private readonly AppDbContext _context;
        private readonly IHubContext<DashboardHub> _hubContext;
        private readonly IEmailService _emailService; // Added IEmailService

        public TicketController(AppDbContext context, IHubContext<DashboardHub> hubContext, IEmailService emailService) // Added IEmailService
        {
            _context = context;
            _hubContext = hubHubContext;
            _emailService = emailService; // Assigned IEmailService
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
            ModelState.Remove("Comments");
            ModelState.Remove("CreatedBy");
            ModelState.Remove("CreatedByUserId");
            ModelState.Remove("CreatedAt");
            ModelState.Remove("CompletedAt");


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

                    // Send email notification for new ticket creation
                    var createdByUser = await _context.Users.FindAsync(currentUserId);
                    var assignedToUser = ticket.Assigned_To.HasValue ? await _context.Users.FindAsync(ticket.Assigned_To.Value) : null;
                    var project = await _context.Project.FindAsync(ticket.Project_Id);


                    var placeholders = new Dictionary<string, string>
                    {
                        { "TicketTitle", ticket.Title },
                        { "TicketDescription", ticket.Description },
                        { "ProjectName", project?.Name ?? "N/A" },
                        { "CreatedBy", createdByUser?.Name ?? "N/A" },
                        { "AssignedTo", assignedToUser?.Name ?? "Unassigned" },
                        { "Priority", ticket.Priority },
                        { "Status", ticket.Status },
                        { "TicketLink", Url.Action("TicketDetails", "Ticket", new { id = ticket.Id }, Request.Scheme) }, // Generate ticket details link
                        { "CurrentYear", DateTime.UtcNow.Year.ToString() }
                    };

                    // Determine recipients (e.g., all project members, assigned user, admin)
                    // For simplicity, let's send to the assigned user and the creator for now.
                    // You might want to expand this logic based on your requirements.
                    var recipientEmails = new List<string>();
                     if (assignedToUser != null && !string.IsNullOrEmpty(assignedToUser.Email))
                    {
                        recipientEmails.Add(assignedToUser.Email);
                    }
                    if (createdByUser != null && !string.IsNullOrEmpty(createdByUser.Email) && (assignedToUser == null || assignedToUser.Id != createdByUser.Id))
                    {
                         recipientEmails.Add(createdByUser.Email);
                    }

                    if (recipientEmails.Any())
                    {
                         var emailBody = await EmailTemplateHelper.LoadTemplateAndPopulateAsync("NewTicketCreated.html", placeholders);
                         // Send email to all relevant recipients
                         foreach (var email in recipientEmails)
                         {
                             await _emailService.SendEmailAsync(email, $"New Ticket Created: {ticket.Title}", emailBody);
                         }
                    }

                    return RedirectToAction("Index", "Dashboard");
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = $"Error creating ticket: {ex.Message}";
                    ViewBag.Projects = new SelectList(await _context.Project.ToListAsync(), "Id", "Name", ticket.Project_Id);
                    ViewBag.Users = new SelectList(await _context.User.ToListAsync(), "Id", "Name", ticket.Assigned_To);
                    ViewBag.TicketStatuses = new SelectList(new List<string> { "To_Do", "In_Progress", "In_Review", "Done" }, ticket.Status);
                    ViewBag.TicketPriorities = new SelectList(new List<string> { "Low", "Medium", "High" }, ticket.Priority);
                    return View(ticket);
                }
            }

            TempData["ErrorMessage"] = "Please correct the errors in the form.";
            ViewBag.Projects = new SelectList(await _context.Project.ToListAsync(), "Id", "Name", ticket.Project_Id);
            ViewBag.Users = new SelectList(await _context.User.ToListAsync(), "Id", "Name", ticket.Assigned_To);
            ViewBag.TicketStatuses = new SelectList(new List<string> { "To_Do", "In_Progress", "In_Review", "Done" }, ticket.Status);
            ViewBag.TicketPriorities = new SelectList(new List<string> { "Low", "Medium", "High" }, ticket.Priority);

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

            var ticket = await _context.Ticket
                                     .Include(t => t.AssignedTo) // Include AssignedTo to get email
                                     .Include(t => t.CreatedBy) // Include CreatedBy to get email
                                     .Include(t => t.Project) // Include Project to get name
                                     .FirstOrDefaultAsync(t => t.Id == ticketId);


            if (ticket == null)
            {
                return NotFound();
            }

            newStatus = newStatus?.Trim();

            var allowedStatuses = new List<string> { "To_Do", "In_Progress", "In_Review", "Done" };
            if (!allowedStatuses.Contains(newStatus))
            {
                TempData["ErrorMessage"] = $"Invalid ticket status provided: '{newStatus}'. Expected one of: {string.Join(", ", allowedStatuses)}.";
                return RedirectToAction(nameof(TicketDetails), new { id = ticketId });
            }

            string oldStatus = ticket.Status;
            ticket.Status = newStatus;
            if (oldStatus != newStatus)
            {
                if (newStatus == "Done")
                {
                    ticket.Completed_At = DateTime.UtcNow;
                }
            }

            try
            {
                _context.Update(ticket);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Ticket '{ticket.Title}' status updated to '{newStatus}' successfully!";

                await _hubContext.Clients.All.SendAsync("ReceiveTicketUpdate", "updated", ticket.Id, oldStatus);

                // Send email notification for status update
                if (oldStatus != newStatus) // Only send email if the status actually changed
                {
                    var updatedByUser = await _context.Users.FindAsync(currentUserId);

                    var placeholders = new Dictionary<string, string>
                    {
                        { "TicketTitle", ticket.Title },
                        { "OldStatus", oldStatus },
                        { "NewStatus", newStatus },
                        { "UpdatedBy", updatedByUser?.Name ?? "N/A" },
                         { "TicketLink", Url.Action("TicketDetails", "Ticket", new { id = ticket.Id }, Request.Scheme) }, // Generate ticket details link
                        { "CurrentYear", DateTime.UtcNow.Year.ToString() }
                    };

                    // Determine recipients (e.g., assigned user, creator, project members)
                    // For simplicity, send to the assigned user and the creator.
                     var recipientEmails = new List<string>();
                     if (ticket.AssignedTo != null && !string.IsNullOrEmpty(ticket.AssignedTo.Email))
                    {
                        recipientEmails.Add(ticket.AssignedTo.Email);
                    }
                    if (ticket.CreatedBy != null && !string.IsNullOrEmpty(ticket.CreatedBy.Email) && (ticket.AssignedTo == null || ticket.AssignedTo.Id != ticket.CreatedBy.Id))
                    {
                         recipientEmails.Add(ticket.CreatedBy.Email);
                    }

                    if (recipientEmails.Any())
                    {
                         var emailBody = await EmailTemplateHelper.LoadTemplateAndPopulateAsync("TicketStatusUpdated.html", placeholders);
                         foreach (var email in recipientEmails)
                         {
                             await _emailService.SendEmailAsync(email, $"Ticket Status Updated: {ticket.Title}", emailBody);
                         }
                    }
                }

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

            var ticket = await _context.Ticket
                                     .Include(t => t.AssignedTo) // Include AssignedTo to get email
                                     .Include(t => t.CreatedBy) // Include CreatedBy to get email
                                     .FirstOrDefaultAsync(t => t.Id == ticketId);


            if (ticket == null)
            {
                TempData["ErrorMessage"] = "Ticket not found.";
                return RedirectToAction("Index", "Dashboard");
            }

            var ticketComment = new Comment
            {
                Ticket_Id = ticketId,
                Created_By = currentUserId,
                Content = commentText.Trim(),
                Created_At = DateTime.UtcNow
            };

            _context.TicketComment.Add(ticketComment);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Comment added successfully!";
            await _hubContext.Clients.All.SendAsync("ReceiveTicketUpdate", "commented", ticket.Id);

            // Send email notification for new comment
            var commentedByUser = await _context.Users.FindAsync(currentUserId);

            var placeholders = new Dictionary<string, string>
            {
                { "TicketTitle", ticket.Title },
                { "CommentedBy", commentedByUser?.Name ?? "N/A" },
                { "CommentContent", ticketComment.Content },
                { "TicketLink", Url.Action("TicketDetails", "Ticket", new { id = ticket.Id }, Request.Scheme) }, // Generate ticket details link
                { "CurrentYear", DateTime.UtcNow.Year.ToString() }
            };

            // Determine recipients (e.g., assigned user, creator, other users who commented)
            // For simplicity, send to the assigned user and the creator.
             var recipientEmails = new List<string>();
             if (ticket.AssignedTo != null && !string.IsNullOrEmpty(ticket.AssignedTo.Email))
            {
                recipientEmails.Add(ticket.AssignedTo.Email);
            }
            if (ticket.CreatedBy != null && !string.IsNullOrEmpty(ticket.CreatedBy.Email) && (ticket.AssignedTo == null || ticket.AssignedTo.Id != ticket.CreatedBy.Id))
            {
                 recipientEmails.Add(ticket.CreatedBy.Email);
            }

             // You might want to add logic here to include other users who have commented on the ticket
             // to the recipient list.

            if (recipientEmails.Any())
            {
                 var emailBody = await EmailTemplateHelper.LoadTemplateAndPopulateAsync("NewCommentAdded.html", placeholders);
                 foreach (var email in recipientEmails)
                 {
                     await _emailService.SendEmailAsync(email, $"New Comment on Ticket: {ticket.Title}", emailBody);
                 }
            }


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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> StatusDragAndDrop([FromBody] ApiControllers.Dtos.UpdateTicketStatusModel model)
        {
            if (model == null)
                return BadRequest(new { success = false, message = "No data received." });

            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdString, out int currentUserId))
                return Unauthorized(new { success = false, message = "User not authenticated." });

            var ticket = await _context.Ticket.FindAsync(model.TicketId);
            if (ticket == null)
                return NotFound(new { success = false, message = $"Ticket with id {model.TicketId} not found." });

            var allowedStatuses = new List<string> { "To_Do", "In_Progress", "In_Review", "Done" };
            var newStatus = model.NewStatus?.Trim();

            if (!allowedStatuses.Contains(newStatus))
                return BadRequest(new
                {
                    success = false,
                    message = $"Invalid status '{newStatus}'. Allowed: {string.Join(", ", allowedStatuses)}"
                });

            var oldStatus = ticket.Status;
            ticket.Status = newStatus;
            if (oldStatus != newStatus && newStatus == "Done")
                ticket.Completed_At = DateTime.UtcNow;

            try
            {
                _context.Update(ticket);
                await _context.SaveChangesAsync();

                // Optional: SignalR notification if you use it
                await _hubContext.Clients.All.SendAsync("ReceiveTicketUpdate", "updated", ticket.Id, oldStatus);

                return Ok(new { success = true, message = "Ticket status updated successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Server error: " + ex.Message });
            }
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
