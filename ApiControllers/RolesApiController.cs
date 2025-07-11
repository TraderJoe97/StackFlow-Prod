using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StackFlow.Data;
using StackFlow.Models;
using StackFlow.ApiControllers.Dtos;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using System; // Required for Exception

namespace StackFlow.ApiControllers
{
    [ApiController]
    [Route("api/roles")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin")] // Only Admin can access role APIs
    public class RolesApiController : ControllerBase
    {
        private readonly AppDbContext _context;

        public RolesApiController(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Gets a list of all roles.
        /// Accessible only by Admin role.
        /// </summary>
        /// <returns>A list of RoleDto objects.</returns>
        [HttpGet] // GET: api/roles
        public async Task<ActionResult<IEnumerable<RoleDto>>> GetAllRoles()
        {
            var roles = await _context.Role.ToListAsync();
            var roleDtos = roles.Select(r => new RoleDto
            {
                Id = r.Id,
                Title = r.Name, // Using Title property as per Role model
                Description = r.Description
            }).ToList();

            return Ok(roleDtos);
        }

        /// <summary>
        /// Gets a specific role by ID.
        /// Accessible only by Admin role.
        /// </summary>
        /// <param name="id">The ID of the role.</param>
        /// <returns>A RoleDto object or 404 Not Found.</returns>
        [HttpGet("{id}")] // GET: api/roles/{id}
        public async Task<ActionResult<RoleDto>> GetRoleById(int id)
        {
            var role = await _context.Role.FindAsync(id);

            if (role == null)
            {
                return NotFound();
            }

            var roleDto = new RoleDto
            {
                Id = role.Id,
                Title = role.Name, // Using Title property as per Role model
                Description = role.Description
            };

            return Ok(roleDto);
        }

        /// <summary>
        /// Creates a new role.
        /// Accessible only by Admin role.
        /// </summary>
        /// <param name="roleDto">The role data (Title and Description).</param>
        /// <returns>A 201 Created response with the new role's data.</returns>
        [HttpPost] // POST: api/roles
        public async Task<ActionResult<RoleDto>> CreateRole([FromBody] RoleDto roleDto)
        {
            if (string.IsNullOrWhiteSpace(roleDto.Title))
            {
                return BadRequest("Role title cannot be empty.");
            }

            if (await _context.Role.AnyAsync(r => r.Name == roleDto.Title))
            {
                return Conflict($"Role with title '{roleDto.Title}' already exists.");
            }

            var role = new Role
            {
                Name = roleDto.Title,
                Description = roleDto.Description
            };

            _context.Role.Add(role);
            await _context.SaveChangesAsync();

            // Return the created role with its new ID
            var createdRoleDto = new RoleDto
            {
                Id = role.Id,
                Title = role.Name,
                Description = role.Description
            };

            return CreatedAtAction(nameof(GetRoleById), new { id = role.Id }, createdRoleDto);
        }

        /// <summary>
        /// Updates an existing role.
        /// Accessible only by Admin role.
        /// </summary>
        /// <param name="id">The ID of the role to update.</param>
        /// <param name="roleDto">The updated role data (Title and Description).</param>
        /// <returns>204 No Content on success, or 404 Not Found, 400 Bad Request, 409 Conflict.</returns>
        [HttpPut("{id}")] // PUT: api/roles/{id}
        public async Task<IActionResult> UpdateRole(int id, [FromBody] RoleDto roleDto)
        {
            var roleToUpdate = await _context.Role.FindAsync(id);
            if (roleToUpdate == null)
            {
                return NotFound("Role not found.");
            }

            if (string.IsNullOrWhiteSpace(roleDto.Title))
            {
                return BadRequest("Role title cannot be empty.");
            }

            // Prevent changing the title of the "Admin" or "Developer" roles
            if (roleToUpdate.Name == "Admin" && roleDto.Title != "Admin")
            {
                return BadRequest("The 'Admin' role title cannot be changed.");
            }
            if (roleToUpdate.Name == "Developer" && roleDto.Title != "Developer")
            {
                return BadRequest("The 'Developer' role title cannot be changed as it is the default registration role.");
            }

            // Check for duplicate title if changing
            if (roleDto.Title != roleToUpdate.Name && await _context.Role.AnyAsync(r => r.Name == roleDto.Title && r.Id != id))
            {
                return Conflict($"Role with title '{roleDto.Title}' already exists.");
            }

            roleToUpdate.Name = roleDto.Title;
            roleToUpdate.Description = roleDto.Description;

            try
            {
                _context.Update(roleToUpdate);
                await _context.SaveChangesAsync();
                return NoContent(); // 204 No Content
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _context.Role.AnyAsync(e => e.Id == id))
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
                return StatusCode(500, $"Error updating role: {ex.Message}");
            }
        }

        /// <summary>
        /// Deletes a role.
        /// Accessible only by Admin role.
        /// Prevents deletion of the "Admin" and "Developer" roles.
        /// When a role is deleted, users previously assigned to it are reassigned to the "Developer" role.
        /// </summary>
        /// <param name="id">The ID of the role to delete.</param>
        /// <returns>204 No Content on success, or 404 Not Found, 400 Bad Request.</returns>
        [HttpDelete("{id}")] // DELETE: api/roles/{id}
        public async Task<IActionResult> DeleteRole(int id)
        {
            var roleToDelete = await _context.Role.Include(r => r.Users).FirstOrDefaultAsync(r => r.Id == id);
            if (roleToDelete == null)
            {
                return NotFound("Role not found.");
            }

            // Prevent deletion of "Admin" and "Developer" roles
            if (roleToDelete.Name == "Admin")
            {
                return BadRequest("The 'Admin' role cannot be deleted.");
            }
            if (roleToDelete.Name == "Developer")
            {
                return BadRequest("The 'Developer' role cannot be deleted as it is the default registration role.");
            }

            // Reassign users from the deleted role to the "Developer" role
            if (roleToDelete.Users.Any())
            {
                var developerRole = await _context.Role.FirstOrDefaultAsync(r => r.Name == "Developer");
                if (developerRole == null)
                {
                    // Log an error or return a specific status if the Developer role is missing
                    return StatusCode(500, "Developer role not found. Cannot reassign users from deleted role.");
                }

                foreach (var user in roleToDelete.Users)
                {
                    user.Role_Id = developerRole.Id; // Reassign to Developer role
                }
                _context.User.UpdateRange(roleToDelete.Users);
            }

            try
            {
                _context.Role.Remove(roleToDelete);
                await _context.SaveChangesAsync();
                return NoContent(); // 204 No Content
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error deleting role: {ex.Message}");
            }
        }
    }
}
