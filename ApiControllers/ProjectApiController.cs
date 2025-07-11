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
    [Route("api/projects")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)] // Requires authentication for all project API calls
    public class ProjectsApiController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ProjectsApiController(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Gets a list of all projects.
        /// Accessible by any authenticated user.
        /// </summary>
        /// <returns>A list of ProjectDto objects.</returns>
        [HttpGet] // GET: api/projects
        public async Task<ActionResult<IEnumerable<ProjectDto>>> GetAllProjects()
        {
            var projects = await _context.Project
                                         .Include(p => p.CreatedBy)
                                         .OrderByDescending(p => p.Start_Date)
                                         .ToListAsync();

            var projectDtos = projects.Select(p => new ProjectDto
            {
                Id = p.Id,
                ProjectName = p.Name, // C# property name
                ProjectDescription = p.Description, // C# property name
                ProjectStartDate = p.Start_Date ?? default(DateTime), // Explicit conversion with null fallback
                ProjectEndDate = p.Due_Date, // C# property name
                ProjectStatus = p.Status, // C# property name
                CreatedByUserId = p.Created_By, // C# property name
                CreatedByUsername = p.CreatedBy?.Name, // C# property name
                TotalTickets = p.Tickets?.Count ?? 0 // Handle null Tickets collection
            }).ToList();

            return Ok(projectDtos);
        }

        /// <summary>
        /// Gets a specific project by ID.
        /// Accessible by any authenticated user.
        /// </summary>
        /// <param name="id">The ID of the project.</param>
        /// <returns>A ProjectDto object or 404 Not Found.</returns>
        [HttpGet("{id}")] // GET: api/projects/{id}
        public async Task<ActionResult<ProjectDto>> GetProjectById(int id)
        {
            var project = await _context.Project
                                        .Include(p => p.CreatedBy)
                                        .Include(p => p.Tickets) // Include tickets to count them
                                        .FirstOrDefaultAsync(p => p.Id == id);

            if (project == null)
            {
                return NotFound();
            }

            var projectDto = new ProjectDto
            {
                Id = project.Id,
                ProjectName = project.Name,
                ProjectDescription = project.Description,
                ProjectStartDate = project.Start_Date ?? default(DateTime),
                ProjectEndDate = project.Due_Date,
                ProjectStatus = project.Status,
                CreatedByUserId = project.Created_By,
                CreatedByUsername = project.CreatedBy?.Name,
                TotalTickets = project.Tickets?.Count ?? 0 // Handle null Tickets collection
            };

            return Ok(projectDto);
        }

        /// <summary>
        /// Creates a new project.
        /// Accessible only by Admin role.
        /// </summary>
        /// <param name="createDto">The project data.</param>
        /// <returns>A 201 Created response with the new project's data.</returns>
        [HttpPost] // POST: api/projects
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ProjectDto>> CreateProject([FromBody] CreateProjectDto createDto)
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdString, out int currentUserId))
            {
                return Unauthorized("User ID not found in claims.");
            }

            var project = new Project
            {
                Name = createDto.ProjectName,
                Description = createDto.ProjectDescription,
                Start_Date = createDto.ProjectStartDate,
                Due_Date = createDto.ProjectEndDate,
                Status = createDto.ProjectStatus,
                Created_By = currentUserId
            };

            _context.Project.Add(project);
            await _context.SaveChangesAsync();

            // Reload project with CreatedBy to get username for DTO
            await _context.Entry(project).Reference(p => p.CreatedBy).LoadAsync();

            var projectDto = new ProjectDto
            {
                Id = project.Id,
                ProjectName = project.Name,
                ProjectDescription = project.Description,
                ProjectStartDate = project.Start_Date ?? default(DateTime),
                ProjectEndDate = project.Due_Date,
                ProjectStatus = project.Status,
                CreatedByUserId = project.Created_By,
                CreatedByUsername = project.CreatedBy?.Name,
                TotalTickets = 0 // Newly created project has no tickets yet
            };

            // CHANGED LINE: Using nameof(GetProjectById)
            return CreatedAtAction(nameof(GetProjectById), new { id = project.Id }, projectDto);
        }

        /// <summary>
        /// Updates an existing project.
        /// Accessible only by Admin role.
        /// </summary>
        /// <param name="id">The ID of the project to update.</param>
        /// <param name="updateDto">The updated project data.</param>
        /// <returns>204 No Content on success, or 404 Not Found.</returns>
        [HttpPut("{id}")] // PUT: api/projects/{id}
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateProject(int id, [FromBody] UpdateProjectDto updateDto)
        {
            var project = await _context.Project.FindAsync(id);
            if (project == null)
            {
                return NotFound();
            }

            // Update properties from DTO using C# model property names
            project.Name = updateDto.ProjectName;
            project.Description = updateDto.ProjectDescription;
            project.Start_Date = updateDto.ProjectStartDate;
            project.Due_Date = updateDto.ProjectEndDate;
            project.Status = updateDto.ProjectStatus;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _context.Project.AnyAsync(e => e.Id == id))
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
                return StatusCode(500, $"Error updating project: {ex.Message}");
            }

            return NoContent(); // 204 No Content
        }

        /// <summary>
        /// Deletes a project and its associated tickets.
        /// Accessible only by Admin role. Use with extreme caution.
        /// </summary>
        /// <param name="id">The ID of the project to delete.</param>
        /// <returns>204 No Content on success, or 404 Not Found.</returns>
        [HttpDelete("{id}")] // DELETE: api/projects/{id}
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteProject(int id)
        {
            var project = await _context.Project
                                        .Include(p => p.Tickets)
                                        .FirstOrDefaultAsync(p => p.Id == id);

            if (project == null)
            {
                return NotFound();
            }

            try
            {
                _context.Ticket.RemoveRange(project.Tickets); // Delete associated tickets
                _context.Project.Remove(project); // Delete the project
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error deleting project: {ex.Message}");
            }

            return NoContent(); // 204 No Content
        }
    }
}