using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StackFlow.ApiControllers.Dtos;
using StackFlow.Data;
using StackFlow.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using StackFlow.Utils;
using StackFlow.Services; // Added for IEmailService

namespace StackFlow.ApiControllers
{
    [ApiController]
    [Route("api/tickets")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)] // Requires authentication for all ticket API calls
    public class TicketsApiController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IEmailService _emailService;

        public TicketsApiController(AppDbContext context, IEmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        /// <summary>
        /// Gets a list of all tickets.
        /// Accessible by any authenticated user.
        /// </summary>
        /// <returns>A list of TicketDto objects.</returns>
        [HttpGet] // GET: api/tickets
        public async Task<ActionResult<IEnumerable<TicketDto>>> GetAllTickets()
        {
            var tickets = await _context.Ticket
                                        .Include(t => t.Project)
                                        .Include(t => t.AssignedTo)
                                        .Include(t => t.CreatedBy)
                                        .OrderByDescending(t => t.Created_At)
                                        .ToListAsync();

            var ticketDtos = tickets.Select(t => new TicketDto
            {
                Id = t.Id,
                Title = t.Title,
                Description = t.Description,
                ProjectId = t.Id,
                ProjectName = t.Project?.Name,
                AssignedToUserId = t.Assigned_To,
                AssignedToUsername = t.AssignedTo?.Name,
                Status = t.Status,
                Priority = t.Priority,
                CreatedByUserId = t.Created_By,
                CreatedByUsername = t.CreatedBy?.Name,
                CreatedAt = t.Created_At,
                DueDate = t.Due_Date,
                CompletedAt = t.Completed_At
            }).ToList();

            return Ok(ticketDtos);
        }

        /// <summary>
        /// Gets a specific ticket by ID.
        /// Accessible by any authenticated user.
        /// </summary>
        /// <param name="id">The ID of the ticket.</param>
        /// <returns>A TicketDto object or 404 Not Found.</returns>
        [HttpGet("{id}")] // GET: api/tickets/{id}
        public async Task<ActionResult<TicketDto>> GetTicketById(int id)
        {
            var ticket = await _context.Ticket
                                     .Include(t => t.Project)
                                     .Include(t => t.AssignedTo)
                                     .Include(t => t.CreatedBy)
                                     .FirstOrDefaultAsync(t => t.Id == id);

            if (ticket == null)
            {
                return NotFound();
            }

            var ticketDto = new TicketDto
            {
                Id = ticket.Id,
                Title = ticket.Title,
                Description = ticket.Description,
                ProjectId = ticket.Id,
                ProjectName = ticket.Project?.Name,
                AssignedToUserId = ticket.Assigned_To,
                AssignedToUsername = ticket.AssignedTo?.Name,
                Status = ticket.Status,
                Priority = ticket.Priority,
                CreatedByUserId = ticket.Created_By,
                CreatedByUsername = ticket.CreatedBy?.Name,
                CreatedAt = ticket.Created_At,
                DueDate = ticket.Due_Date,
                CompletedAt = ticket.Completed_At
            };

            return Ok(ticketDto);
        }

        /// <summary>
        /// Creates a new ticket.
        /// Accessible by Admin and Project Manager roles.
        /// </summary>
        /// <param name="createDto">The ticket data.</param>
        /// <returns>A 201 Created response with the new ticket's data.</returns>
        [HttpPost] // POST: api/tickets
        [Authorize(Roles = "Admin,Project Manager")]
        public async Task<ActionResult<TicketDto>> CreateTicket([FromBody] CreateTicketDto createDto)
        {
            // Validate the create DTO
            if (createDto == null)
            {
                return BadRequest("Ticket creation data cannot be null.");
            }
            if (string.IsNullOrWhiteSpace(createDto.Title) || createDto.Title.Length > 255)
            {
                return BadRequest("Ticket title is required and must be less than 255 characters.");
            }
   
            // Enum validation using IsDefined is fine, but consider if you need specific handling for invalid enum values
            if (!Enum.IsDefined(typeof(TicketStatus), createDto.Status))
            {
                 // You might want to return a more specific error or a list of valid statuses
                return BadRequest($"Invalid ticket status: {createDto.Status}. Valid statuses are: {string.Join(", ", Enum.GetNames(typeof(TicketStatus)))}");
            }

            // Check if project exists
            var project = await _context.Project.FindAsync(createDto.ProjectId);
            if (project == null)
            {
                return BadRequest("Project with ID " + createDto.ProjectId + " does not exist");
            }

            // Check if Assigned User exists and is active/verified
            User assignedUser = null;
            if (createDto.AssignedToUserId.HasValue)
            {
                assignedUser = await _context.User.FindAsync(createDto.AssignedToUserId.Value);
                if (assignedUser == null)
                {
                    return BadRequest("Assigned user does not exist.");
                }
                if (assignedUser.IsDeleted)
                {
                    return BadRequest("Assigned user is no longer active.");
                }
                if (!assignedUser.IsVerified)
                {
                     // Depending on your business logic, you might allow assigning to unverified users
                     // or return a different error. This currently returns BadRequest.
                     return BadRequest("Assigned user account not verified.");
                }
            }

            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdString, out int currentUserId))
            {
                return Unauthorized("User ID not found in claims.");
            }

            var createdByUser = await _context.User.FindAsync(currentUserId);
             if (createdByUser == null || createdByUser.IsDeleted || !createdByUser.IsVerified)
            {
                 return Unauthorized("Creating user is invalid or inactive.");
            }


            var ticket = new Ticket
            {
                Title = createDto.Title,
                Description = createDto.Description,
                Project_Id = createDto.ProjectId,
                Assigned_To = createDto.AssignedToUserId,
                Status = createDto.Status,
                Priority = createDto.Priority,
                Due_Date = createDto.DueDate, // Allow nullability
                Created_By = currentUserId,
                Created_At = DateTime.UtcNow
            };

            _context.Ticket.Add(ticket);
            await _context.SaveChangesAsync();

            // Reload ticket with navigation properties for email and DTO
            await _context.Entry(ticket).Reference(t => t.Project).LoadAsync();
            await _context.Entry(ticket).Reference(t => t.AssignedTo).LoadAsync();
            await _context.Entry(ticket).Reference(t => t.CreatedBy).LoadAsync();

            // Send email notification for new ticket creation
            try
            {
                var placeholders = new Dictionary<string, string>
                {
                    { "TicketTitle", ticket.Title },
                    { "TicketDescription", ticket.Description },
                    { "ProjectName", ticket.Project?.Name ?? "N/A" },
                    { "CreatedBy", ticket.CreatedBy?.Name ?? "N/A" },
                    { "AssignedTo", ticket.AssignedTo?.Name ?? "Unassigned" },
                    { "Priority", ticket.Priority },
                    { "Status", ticket.Status },
                     // Consider generating a link to the ticket details page if applicable
                    { "TicketLink", $"{Request.Scheme}://{Request.Host}/Ticket/TicketDetails/{ticket.Id}" },
                    { "CurrentYear", DateTime.UtcNow.Year.ToString() }
                };

                var recipientEmails = new List<string>();
                if (ticket.AssignedTo != null && !string.IsNullOrEmpty(ticket.AssignedTo.Email) && !ticket.AssignedTo.IsDeleted && ticket.AssignedTo.IsVerified)
                {
                    recipientEmails.Add(ticket.AssignedTo.Email);
                }
                 // Also notify the creator if they are different from the assignee and their email is valid/verified
                if (ticket.CreatedBy != null && !string.IsNullOrEmpty(ticket.CreatedBy.Email) && !ticket.CreatedBy.IsDeleted && ticket.CreatedBy.IsVerified && (ticket.AssignedTo == null || ticket.AssignedTo.Id != ticket.CreatedBy.Id))
                {
                     recipientEmails.Add(ticket.CreatedBy.Email);
                }

                if (recipientEmails.Any())
                {
                    var emailBody = await EmailTemplateHelper.LoadTemplateAndPopulateAsync("NewTicketCreated.html", placeholders);
                    // Send email to all relevant recipients
                    foreach (var email in recipientEmails)
                    {
                         // Add a check to ensure the recipient email is valid if necessary
                        await _emailService.SendEmailAsync(email, $"New Ticket Created: {ticket.Title}", emailBody);
                    }
                }
            }
            catch (Exception ex)
            {
                // Log error, but don't prevent ticket creation
                Console.WriteLine($"Error sending new ticket email: {ex.Message}");
            }

            var ticketDto = new TicketDto
            {
                Id = ticket.Id,
                Title = ticket.Title,
                Description = ticket.Description,
                ProjectId = ticket.Project_Id,
                ProjectName = ticket.Project?.Name,
                AssignedToUserId = ticket.Assigned_To,
                AssignedToUsername = ticket.AssignedTo?.Name,
                Status = ticket.Status,
                Priority = ticket.Priority,
                CreatedByUserId = ticket.Created_By,
                CreatedByUsername = ticket.CreatedBy?.Name,
                CreatedAt = ticket.Created_At,
                DueDate = ticket.Due_Date,
                CompletedAt = ticket.Completed_At
            };

            return CreatedAtAction(nameof(GetTicketById), new { id = ticket.Id }, ticketDto);
        }

        /// <summary>
        /// Updates an existing ticket.
        /// Accessible by Admin and Project Manager roles.
        /// </summary>
        /// <param name="id">The ID of the ticket to update.</param>
        /// <param name="updateDto">The updated ticket data.</param>
        /// <returns>204 No Content on success, or 404 Not Found.</returns>
        [HttpPut("{id}")] // PUT: api/tickets/{id}
        [Authorize(Roles = "Admin,Project Manager")]
        public async Task<IActionResult> UpdateTicket(int id, [FromBody] UpdateTicketDto updateDto)
        {
            // Validate the update DTO
            if (updateDto == null)
            {
                return BadRequest("Ticket update data cannot be null.");
            }
            if (string.IsNullOrWhiteSpace(updateDto.Title) || updateDto.Title.Length > 255)
            {
                return BadRequest("Ticket title is required and must be less than 255 characters.");
            }
            // Enum validation using IsDefined is fine, but consider if you need specific handling for invalid enum values
            if (!Enum.IsDefined(typeof(TicketStatus), updateDto.Status))
            {
                 // You might want to return a more specific error or a list of valid statuses
                 return BadRequest($"Invalid ticket status: {updateDto.Status}. Valid statuses are: {string.Join(", ", Enum.GetNames(typeof(TicketStatus)))}");
            }
            // Check if project exists
            var project = await _context.Project.FindAsync(updateDto.ProjectId);
            if (project == null)
            {
                return BadRequest("Project with ID " + updateDto.ProjectId + " does not exist");
            }

            // Check if Assigned User exists and is active/verified
            User assignedUser = null;
            if (updateDto.AssignedToUserId.HasValue)
            {
                assignedUser = await _context.User.FindAsync(updateDto.AssignedToUserId.Value);
                if (assignedUser == null)
                {
                    return BadRequest("Assigned user does not exist.");
                }
                if (assignedUser.IsDeleted)
                {
                    return BadRequest("Assigned user is no longer active.");
                }
                 if (!assignedUser.IsVerified)
                 {
                      // Depending on your business logic, you might allow assigning to unverified users
                      // or return a different error. This currently returns BadRequest.
                      return BadRequest("Assigned user account not verified.");
                 }
            }

            var ticket = await _context.Ticket
                                     .Include(t => t.AssignedTo)
                                     .Include(t => t.CreatedBy)
                                     .Include(t => t.Project)
                                     .FirstOrDefaultAsync(t => t.Id == id);

            if (ticket == null)
            {
                return NotFound();
            }

            // Store old values before updating
            var oldAssignedToUserId = ticket.Assigned_To;
            var oldStatus = ticket.Status;
            var oldPriority = ticket.Priority;
            var oldDueDate = ticket.Due_Date;

            // Update properties from DTO
            ticket.Title = updateDto.Title;
            ticket.Description = updateDto.Description;
            ticket.Project_Id = updateDto.ProjectId; // Corrected ProjectId assignment
            ticket.Assigned_To = updateDto.AssignedToUserId;
            ticket.Status = updateDto.Status;
            ticket.Priority = updateDto.Priority;
            ticket.Due_Date = updateDto.DueDate; // Allow nullability
            ticket.Completed_At = updateDto.CompletedAt; // Allow nullability

             // Update Completed_At if status changes to Done
            if (oldStatus != ticket.Status && ticket.Status == "Done")
            {
                ticket.Completed_At = DateTime.UtcNow;
            } else if (oldStatus != ticket.Status && oldStatus == "Done" && ticket.Status != "Done")
            {
                 // If status changes from Done to something else, clear Completed_At
                ticket.Completed_At = null;
            }

            try
            {
                await _context.SaveChangesAsync();

                // Send email notifications based on changes
                var updatedByUser = await _context.User.FindAsync(int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)));

                // Notification for assigned user change
                if (oldAssignedToUserId != ticket.Assigned_To)
                {
                    var newlyAssignedUser = await _context.User.FindAsync(ticket.Assigned_To);
                    if (newlyAssignedUser != null && !string.IsNullOrEmpty(newlyAssignedUser.Email) && !newlyAssignedUser.IsDeleted && newlyAssignedUser.IsVerified)
                    {
                        try
                        {
                             var placeholders = new Dictionary<string, string>
                             {
                                 { "TicketTitle", ticket.Title },
                                 { "ProjectName", ticket.Project?.Name ?? "N/A" },
                                 { "AssignedTo", newlyAssignedUser.Name ?? "Unassigned" },
                                  { "TicketLink", $"{Request.Scheme}://{Request.Host}/Ticket/TicketDetails/{ticket.Id}" },
                                 { "CurrentYear", DateTime.UtcNow.Year.ToString() }
                             };
                             var emailBody = await EmailTemplateHelper.LoadTemplateAndPopulateAsync("TicketAssigned.html", placeholders);
                             await _emailService.SendEmailAsync(newlyAssignedUser.Email, $"Ticket Assigned to You: {ticket.Title}", emailBody);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error sending ticket assignment email: {ex.Message}");
                        }
                    }
                }

                // Notification for status change
                if (oldStatus != ticket.Status)
                {
                     var placeholders = new Dictionary<string, string>
                    {
                        { "TicketTitle", ticket.Title },
                        { "OldStatus", oldStatus },
                        { "NewStatus", ticket.Status },
                        { "UpdatedBy", updatedByUser?.Name ?? "N/A" },
                        { "TicketLink", $"{Request.Scheme}://{Request.Host}/Ticket/TicketDetails/{ticket.Id}" },
                        { "CurrentYear", DateTime.UtcNow.Year.ToString() }
                    };

                    var recipientEmails = new List<string>();
                     // Notify the assignee (if any and different from updater)
                    if (ticket.AssignedTo != null && !string.IsNullOrEmpty(ticket.AssignedTo.Email) && ticket.AssignedTo.Id != updatedByUser.Id && !ticket.AssignedTo.IsDeleted && ticket.AssignedTo.IsVerified)
                    {
                         recipientEmails.Add(ticket.AssignedTo.Email);
                    }
                     // Notify the creator (if any and different from updater and assignee)
                     if (ticket.CreatedBy != null && !string.IsNullOrEmpty(ticket.CreatedBy.Email) && ticket.CreatedBy.Id != updatedByUser.Id && (ticket.AssignedTo == null || ticket.AssignedTo.Id != ticket.CreatedBy.Id) && !ticket.CreatedBy.IsDeleted && ticket.CreatedBy.IsVerified)
                    {
                         recipientEmails.Add(ticket.CreatedBy.Email);
                    }
                     // You might also want to notify other relevant users like project members

                     if (recipientEmails.Any())
                    {
                         var emailBody = await EmailTemplateHelper.LoadTemplateAndPopulateAsync("TicketStatusUpdated.html", placeholders);
                         foreach (var email in recipientEmails)
                         {
                             await _emailService.SendEmailAsync(email, $"Ticket Status Updated: {ticket.Title}", emailBody);
                         }
                    }
                }

                // You could add similar logic for priority or due date changes if needed

            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _context.Ticket.AnyAsync(e => e.Id == id))
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
                return StatusCode(500, $"Error updating ticket: {ex.Message}");
            }

            return NoContent(); // 204 No Content
        }

        /// <summary>
        /// Deletes a ticket.
        /// Accessible by Admin and Project Manager roles.
        /// </summary>
        /// <param name="id">The ID of the ticket to delete.</param>
        /// <returns>204 No Content on success, or 404 Not Found.</returns>
        [HttpDelete("{id}")] // DELETE: api/tickets/{id}
        [Authorize(Roles = "Admin,Project Manager")]
        public async Task<IActionResult> DeleteTicket(int id)
        {
            var ticket = await _context.Ticket.FindAsync(id);
            if (ticket == null)
            {
                return NotFound();
            }

            try
            {
                _context.Ticket.Remove(ticket);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error deleting ticket: {ex.Message}");
            }

            return NoContent(); // 204 No Content
        }
    }
}
