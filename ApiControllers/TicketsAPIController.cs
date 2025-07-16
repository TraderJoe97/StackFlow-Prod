using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
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

namespace StackFlow.ApiControllers
{
    [ApiController]
    [Route("api/tickets")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)] // Requires authentication for all ticket API calls
    public class TicketsApiController : ControllerBase
    {
        private readonly AppDbContext _context;

        public TicketsApiController(AppDbContext context)
        {
            _context = context;
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

            //validate the create DTO
            if (createDto == null)
            {
                return BadRequest("Ticket creation data cannot be null.");
            }
            if (string.IsNullOrWhiteSpace(createDto.Title) || createDto.Title.Length > 255)
            {
                return BadRequest("Ticket title is required and must be less than 255 characters.");
            }
   
            if (!Enum.IsDefined(typeof(TicketStatus), createDto.Status))
            {
                return BadRequest("Invalid ticket status.");
            }

            // Check if project exists
            var project = await _context.Project.FindAsync(createDto.ProjectId);
            if (project == null)
            {
                return BadRequest("Project with ID " + createDto.ProjectId + " does not exist");
            }

            // Check if Assigned Usser exists

            var AssignedUser = await _context.User.FindAsync(createDto.AssignedToUserId);
            if (AssignedUser == null)
            {
                return BadRequest("Assigned user Does not Exist");
            } else if (AssignedUser.IsDeleted) 
            {
                return BadRequest("Assigned user is no longer active");
            } else if (AssignedUser.IsVerified)
            {
                return BadRequest("Assigned user account not Verified.");
            }


                var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdString, out int currentUserId))
            {
                return Unauthorized("User ID not found in claims.");
            }

            var ticket = new Ticket
            {
                Title = createDto.Title,
                Description = createDto.Description,
                Project_Id = createDto.ProjectId,
                Assigned_To = createDto.AssignedToUserId,
                Status = createDto.Status,
                Priority = createDto.Priority,
                Due_Date = createDto.DueDate ?? default(DateTime), // Fix for CS0266 and CS8629
                Created_By = currentUserId,
                Created_At = DateTime.UtcNow
            };

            _context.Ticket.Add(ticket);
            await _context.SaveChangesAsync();

            // Reload ticket with navigation properties to populate DTO
            await _context.Entry(ticket).Reference(t => t.Project).LoadAsync();
            await _context.Entry(ticket).Reference(t => t.AssignedTo).LoadAsync();
            await _context.Entry(ticket).Reference(t => t.CreatedBy).LoadAsync();

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
            //validate the update DTO
            if (updateDto == null)
            {
                return BadRequest("Ticket update data cannot be null.");
            }
            if (string.IsNullOrWhiteSpace(updateDto.Title) || updateDto.Title.Length > 255)
            {
                return BadRequest("Ticket title is required and must be less than 255 characters.");
            }
            if (!Enum.IsDefined(typeof(TicketStatus), updateDto.Status))
            {
                return BadRequest("Invalid ticket status.");
            }
            // Check if project exists
            var project = await _context.Project.FindAsync(updateDto.ProjectId);
            if (project == null)
            {
                return BadRequest("Project with ID " + updateDto.ProjectId + " does not exist");
            }

            // Check if Assigned Usser exists

            var AssignedUser = await _context.User.FindAsync(updateDto.AssignedToUserId);
            if (AssignedUser == null)
            {
                return BadRequest("Assigned user Does not Exist");
            }
            else if (AssignedUser.IsDeleted)
            {
                return BadRequest("Assigned user is no longer active");
            }
            else if (AssignedUser.IsVerified)
            {
                return BadRequest("Assigned user account not Verified.");
            }


            var ticket = await _context.Ticket.FindAsync(id);
            if (ticket == null)
            {
                return NotFound();
            }

            // Update properties from DTO using C# model property names
            ticket.Title = updateDto.Title;
            ticket.Description = updateDto.Description;
            ticket.Id = updateDto.ProjectId;
            ticket.Assigned_To = updateDto.AssignedToUserId;
            ticket.Status = updateDto.Status;
            ticket.Priority = updateDto.Priority;
            ticket.Due_Date = updateDto.DueDate ?? default(DateTime); // Handle nullable DateTime
            ticket.Completed_At = updateDto.CompletedAt ?? default(DateTime); // Handle nullable DateTime

            try
            {
                await _context.SaveChangesAsync();
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
