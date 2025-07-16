using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StackFlow.Data;
using StackFlow.Models;
using StackFlow.ApiControllers.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace StackFlow.ApiControllers
{
    [ApiController]
    [Route("api/tickets")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)] // Requires JWT authentication for all endpoints
    public class TicketsApiController : ControllerBase
    {
        private readonly AppDbContext _context;

        public TicketsApiController(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Retrieves a list of all tickets from the database.
        /// Accessible by any authenticated user.
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<TicketDto>>> GetAllTickets()
        {
            // Retrieve tickets with their related project, assigned user, and creator
            var tickets = await _context.Ticket
                                        .Include(t => t.Project)
                                        .Include(t => t.AssignedTo)
                                        .Include(t => t.CreatedBy)
                                        .OrderByDescending(t => t.Created_At)
                                        .ToListAsync();

            // Convert each ticket to a DTO
            var ticketDtos = tickets.Select(t => new TicketDto
            {
                Id = t.Id,
                Title = t.Title,
                Description = t.Description,
                ProjectId = t.Project_Id,
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
        /// Retrieves a single ticket by its ID.
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<TicketDto>> GetTicketById(int id)
        {
            // Find the ticket and include its related data
            var ticket = await _context.Ticket
                                       .Include(t => t.Project)
                                       .Include(t => t.AssignedTo)
                                       .Include(t => t.CreatedBy)
                                       .FirstOrDefaultAsync(t => t.Id == id);

            if (ticket == null)
                return NotFound();

            // Convert ticket to DTO
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

            return Ok(ticketDto);
        }

        /// <summary>
        /// Creates a new ticket in the system.
        /// Only accessible by Admin and Project Manager.
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin,Project Manager")]
        public async Task<ActionResult<TicketDto>> CreateTicket([FromBody] CreateTicketDto createDto)
        {
            // Get the current logged-in user's ID from claims
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdString, out int currentUserId))
                return Unauthorized("User ID not found in claims.");

            // Create a new ticket entity
            var ticket = new Ticket
            {
                Title = createDto.Title,
                Description = createDto.Description,
                Project_Id = createDto.ProjectId,
                Assigned_To = createDto.AssignedToUserId,
                Status = createDto.Status,
                Priority = createDto.Priority,
                Due_Date = createDto.DueDate ?? DateTime.UtcNow.AddDays(7),
                Created_By = currentUserId,
                Created_At = DateTime.UtcNow
            };

            // Add ticket to database
            _context.Ticket.Add(ticket);
            await _context.SaveChangesAsync();

            // Load related data
            await _context.Entry(ticket).Reference(t => t.Project).LoadAsync();
            await _context.Entry(ticket).Reference(t => t.AssignedTo).LoadAsync();
            await _context.Entry(ticket).Reference(t => t.CreatedBy).LoadAsync();

            // Convert to DTO
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
        /// Only accessible by Admin and Project Manager.
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin,Project Manager")]
        public async Task<IActionResult> UpdateTicket(int id, [FromBody] UpdateTicketDto updateDto)
        {
            // Find the ticket
            var ticket = await _context.Ticket.FindAsync(id);
            if (ticket == null)
                return NotFound();

            // Update ticket properties from DTO
            ticket.Title = updateDto.Title;
            ticket.Description = updateDto.Description;
            ticket.Project_Id = updateDto.ProjectId;
            ticket.Assigned_To = updateDto.AssignedToUserId;
            ticket.Status = updateDto.Status;
            ticket.Priority = updateDto.Priority;
            ticket.Due_Date = updateDto.DueDate ?? ticket.Due_Date;
            ticket.Completed_At = updateDto.CompletedAt ?? ticket.Completed_At;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _context.Ticket.AnyAsync(t => t.Id == id))
                    return NotFound();
                else
                    throw;
            }

            return NoContent(); // Success with no return data
        }

        /// <summary>
        /// Deletes a ticket from the system.
        /// Only accessible by Admin and Project Manager.
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin,Project Manager")]
        public async Task<IActionResult> DeleteTicket(int id)
        {
            var ticket = await _context.Ticket.FindAsync(id);
            if (ticket == null)
                return NotFound();

            try
            {
                _context.Ticket.Remove(ticket);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error deleting ticket: {ex.Message}");
            }

            return NoContent();
        }
    }
}
