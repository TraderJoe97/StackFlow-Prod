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
    [Route("api/ticketcomments")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class TicketCommentsApiController : ControllerBase

                private readonly AppDbContext _context;

    public TicketCommentsApiController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet("/api/tickets/{ticketId}/comments")]
    public async Task<ActionResult<IEnumerable<TicketCommentDto>>> GetCommentsForTicket(int ticketId)
    {
        var comments = await _context.TicketComment
                                     .Where(tc => tc.Ticket_Id == ticketId) // FIXED: was tc.Id == ticketId
                                     .Include(tc => tc.CreatedBy)
                                     .OrderBy(tc => tc.Created_At)
                                     .ToListAsync();

        var commentDtos = comments.Select(tc => new TicketCommentDto
        {
            Id = tc.Id,
            TicketId = tc.Ticket_Id,
            UserId = tc.Created_By,
            Username = tc.CreatedBy.Name,
            CommentText = tc.Content,
            CommentCreatedAt = tc.Created_At
        }).ToList();

        return Ok(commentDtos);
    }
    [HttpGet("{id}")]
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

    [HttpPost("/api/tickets/{ticketId}/comments")]
    public async Task<ActionResult<TicketCommentDto>> AddCommentToTicket(int ticketId, [FromBody] CreateTicketCommentDto createDto)
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdString, out int currentUserId))
        {
            return Unauthorized("User ID not found in claims.");
        }

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

    [HttpPut("{id}")]
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

        return NoContent();
    }


