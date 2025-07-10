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


