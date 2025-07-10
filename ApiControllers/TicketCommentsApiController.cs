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
    [Route("api/ticketcomments")] // Base route for individual comment operations
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)] // Requires authentication for all comment API calls
    public class TicketCommentsApiController : ControllerBase
    {
        private readonly AppDbContext _context;

        public TicketCommentsApiController(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Gets all comments for a specific ticket.
        /// Accessible by any authenticated user.
        /// </summary>
        /// <param name="ticketId">The ID of the ticket.</param>
        /// <returns>A list of TicketCommentDto objects.</returns>
        [HttpGet("/api/tickets/{ticketId}/comments")] // GET: api/tickets/{ticketId}/comments
        public async Task<ActionResult<IEnumerable<TicketCommentDto>>> GetCommentsForTicket(int ticketId)
        {
            var comments = await _context.TicketComment
                                         .Where(tc => tc.Id == ticketId)
                                         .Include(tc => tc.CreatedBy) // Include User for Username
                                         .OrderBy(tc => tc.Created_At)
                                         .ToListAsync();

            var commentDtos = comments.Select(tc => new TicketCommentDto
            {
                Id = tc.Id,
                TicketId = tc.Id,
                UserId = tc.Created_By, // Using UserId property as per TicketComment model
                Username = tc.CreatedBy.Name, // Using Username property from User model
                CommentText = tc.Content, // Using CommentText property as per TicketComment model
                CommentCreatedAt = tc.Created_At
            }).ToList();

            return Ok(commentDtos);
        }

        /// <summary>
        /// Gets a specific ticket comment by ID.
        /// Accessible by any authenticated user.
        /// </summary>
        /// <param name="id">The ID of the comment.</param>
        /// <returns>A TicketCommentDto object or 404 Not Found.</returns>
        [HttpGet("{id}")] // GET: api/ticketcomments/{id}
        public async Task<ActionResult<TicketCommentDto>> GetTicketCommentById(int id)
        {
            var comment = await _context.TicketComment
                                        .Include(tc => tc.CreatedBy)
                                        .FirstOrDefaultAsync(tc => tc.Id == id);

            if (comment == null)
            {
                return NotFound();
            }

            var commentDto = new TicketCommentDto
            {
                Id = comment.Id,
                TicketId = comment.Ticket_Id,
                UserId = comment.Created_By,
                Username = comment.CreatedBy?.Name,
                CommentText = comment.Content,
                CommentCreatedAt = comment.Created_At
            };

            return Ok(commentDto);
        }

        /// <summary>
        /// Adds a new comment to a specific ticket.
        /// Accessible by any authenticated user.
        /// </summary>
        /// <param name="ticketId">The ID of the ticket to add the comment to.</param>
        /// <param name="createDto">The comment data.</param>
        /// <returns>A 201 Created response with the new comment's data.</returns>
        [HttpPost("/api/tickets/{ticketId}/comments")] // POST: api/tickets/{ticketId}/comments
        public async Task<ActionResult<TicketCommentDto>> AddCommentToTicket(int ticketId, [FromBody] CreateTicketCommentDto createDto)
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdString, out int currentUserId))
            {
                return Unauthorized("User ID not found in claims.");
            }

            // Ensure the ticket exists
            var ticketExists = await _context.Ticket.AnyAsync(t => t.Id == ticketId);
            if (!ticketExists)
            {
                return NotFound($"Ticket with ID {ticketId} not found.");
            }

            var comment = new Comment
            {
                Ticket_Id = ticketId,
                Created_By = currentUserId,
                Content = createDto.CommentText.Trim(),
                Created_At = DateTime.UtcNow
            };

            _context.TicketComment.Add(comment);
            await _context.SaveChangesAsync();

            // Reload comment with User to get username for DTO
            await _context.Entry(comment).Reference(c => c.CreatedBy).LoadAsync();

            var commentDto = new TicketCommentDto
            {
                Id = comment.Id,
                TicketId = comment.Ticket_Id,
                UserId = comment.Created_By,
                Username = comment.CreatedBy?.Name,
                CommentText = comment.Content,
                CommentCreatedAt = comment.Created_At
            };

            return CreatedAtAction(nameof(GetTicketCommentById), new { id = comment.Id }, commentDto);
        }

        /// <summary>
        /// Updates an existing ticket comment.
        /// Accessible only by the user who created the comment, or an Admin.
        /// </summary>
        /// <param name="id">The ID of the comment to update.</param>
        /// <param name="updateDto">The updated comment data.</param>
        /// <returns>204 No Content on success, or 404 Not Found/403 Forbidden.</returns>
        [HttpPut("{id}")] // PUT: api/ticketcomments/{id}
        public async Task<IActionResult> UpdateTicketComment(int id, [FromBody] UpdateTicketCommentDto updateDto)
        {
            var comment = await _context.TicketComment.FindAsync(id);
            if (comment == null)
            {
                return NotFound();
            }

            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdString, out int currentUserId))
            {
                return Unauthorized("User ID not found in claims.");
            }

            // Check if the current user is the owner of the comment OR an Admin
            if (comment.Created_By != currentUserId && !User.IsInRole("Admin"))
            {
                return Forbid("You do not have permission to update this comment.");
            }

            comment.Content = updateDto.CommentText.Trim();

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _context.TicketComment.AnyAsync(e => e.Id == id))
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
                return StatusCode(500, $"Error updating comment: {ex.Message}");
            }

            return NoContent(); // 204 No Content
        }

        /// <summary>
        /// Deletes a ticket comment.
        /// Accessible only by the user who created the comment, or an Admin.
        /// </summary>
        /// <param name="id">The ID of the comment to delete.</param>
        /// <returns>204 No Content on success, or 404 Not Found/403 Forbidden.</returns>
        [HttpDelete("{id}")] // DELETE: api/ticketcomments/{id}
        public async Task<IActionResult> DeleteTicketComment(int id)
        {
            var comment = await _context.TicketComment.FindAsync(id);
            if (comment == null)
            {
                return NotFound();
            }

            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdString, out int currentUserId))
            {
                return Unauthorized("User ID not found in claims.");
            }

            // Check if the current user is the owner of the comment OR an Admin
            if (comment.Created_By != currentUserId && !User.IsInRole("Admin"))
            {
                return Forbid("You do not have permission to delete this comment.");
            }

            try
            {
                _context.TicketComment.Remove(comment);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error deleting comment: {ex.Message}");
            }

            return NoContent(); // 204 No Content
        }
    }
}

