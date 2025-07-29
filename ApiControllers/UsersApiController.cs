using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StackFlow.Data;
using StackFlow.Models;
using BCrypt.Net;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System;
using Microsoft.AspNetCore.Authentication.JwtBearer; // Added for JWT Bearer authentication
using StackFlow.ApiControllers.Dtos;
using StackFlow.Utils;

namespace StackFlow.ApiControllers
{
    // Enables API-specific behaviors like automatic model validation, content negotiation.
    [ApiController]
    // Sets the base route for all actions in this controller to /api/users
    [Route("api/users")]
    // By default, all actions in this controller require an authenticated user
    // using the JWT Bearer scheme.
    // Specific actions can override this with [AllowAnonymous] or different [Authorize] roles/schemes.
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class UsersApiController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IEmailService _emailService;

        public UsersApiController(AppDbContext context, IEmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        public class UserQueryParameters
        {
            public int Page { get; set; } = 1;
            public int PageSize { get; set; } = 10;
            public string SearchQuery { get; set; } = "";
        }

        // --- API Endpoints ---

        // Removed Register endpoint (moved to AccountApiController)

        /// <summary>
        /// Gets a list of all active (verified and not deleted) users with pagination and optional search.

        /// </summary>
        /// <param name="query">Pagination and search parameters.</param>
        /// <returns>A paginated list of UserDto objects.</returns>
        [HttpGet] // Route: /api/users
        public async Task<ActionResult<IEnumerable<UserDto>>> GetAllUsers([FromQuery] UserQueryParameters query)
        {
            query.Page = Math.Max(1, query.Page);
            query.PageSize = Math.Max(1, query.PageSize);

            var usersQuery = _context.User
                                     .Where(u => !u.IsDeleted && u.IsVerified) // Only active users
                                     .Include(u => u.Role)
                                     .AsQueryable();

            if (!string.IsNullOrWhiteSpace(query.SearchQuery))
            {
                var searchQueryLower = query.SearchQuery.ToLower();
                usersQuery = usersQuery.Where(u =>
                    u.Name.ToLower().Contains(searchQueryLower) ||
                    u.Email.ToLower().Contains(searchQueryLower) ||
                    (u.Role != null && u.Role.Name.ToLower().Contains(searchQueryLower)));
            }

            var totalUsers = await usersQuery.CountAsync();
            Response.Headers.Add("X-Total-Count", totalUsers.ToString());
            Response.Headers.Add("X-Page-Size", query.PageSize.ToString());
            Response.Headers.Add("X-Current-Page", query.Page.ToString());
            Response.Headers.Add("X-Total-Pages", Math.Ceiling((double)totalUsers / query.PageSize).ToString());

            var users = await usersQuery
                                .OrderBy(u => u.Name)
                                .Skip((query.Page - 1) * query.PageSize)
                                .Take(query.PageSize)
                                .ToListAsync();

            var userDtos = users.Select(u => new UserDto
            {
                Id = u.Id,
                Name = u.Name,
                Email = u.Email,
                Role = u.Role?.Name,
                IsVerified = u.IsVerified,
                IsDeleted = u.IsDeleted,
                Created_At = u.Created_At
            }).ToList();

            return Ok(userDtos); // 200 OK
        }

        /// <summary>
        /// Gets a list of users pending verification with pagination.
        /// </summary>
        /// <param name="query">Pagination parameters.</param>
        /// <returns>A paginated list of UserDto objects.</returns>
        [HttpGet("pending")] // Route: /api/users/pending
        public async Task<ActionResult<IEnumerable<UserDto>>> GetPendingUsers([FromQuery] UserQueryParameters query)
        {
            query.Page = Math.Max(1, query.Page);
            query.PageSize = Math.Max(1, query.PageSize);

            var pendingUsersQuery = _context.User
                                            .Where(u => !u.IsDeleted && !u.IsVerified) // Only pending users
                                            .Include(u => u.Role)
                                            .AsQueryable();

            var totalPendingUsers = await pendingUsersQuery.CountAsync();
            Response.Headers.Add("X-Total-Count", totalPendingUsers.ToString());
            Response.Headers.Add("X-Page-Size", query.PageSize.ToString());
            Response.Headers.Add("X-Current-Page", query.Page.ToString());
            Response.Headers.Add("X-Total-Pages", Math.Ceiling((double)totalPendingUsers / query.PageSize).ToString());

            var pendingUsers = await pendingUsersQuery
                                .OrderBy(u => u.Created_At)
                                .Skip((query.Page - 1) * query.PageSize)
                                .Take(query.PageSize)
                                .ToListAsync();

            var userDtos = pendingUsers.Select(u => new UserDto
            {
                Id = u.Id,
                Name = u.Name,
                Email = u.Email,
                Role = u.Role?.Name,
                IsVerified = u.IsVerified,
                IsDeleted = u.IsDeleted,
                Created_At = u.Created_At
            }).ToList();

            return Ok(userDtos); // 200 OK
        }

        /// <summary>
        /// Gets a specific user by ID.
        /// </summary>
        /// <param name="id">The ID of the user.</param>
        /// <returns>A UserDto object or 404 Not Found.</returns>
        [HttpGet("{id}")] // Route: /api/users/{id}
        public async Task<ActionResult<UserDto>> GetUserById(int id)
        {
            var user = await _context.User
                                     .Where(u => u.Id == id && !u.IsDeleted) // Ensure user exists and is not deleted
                                     .Include(u => u.Role)
                                     .FirstOrDefaultAsync();

            if (user == null)
            {
                return NotFound(); // 404 Not Found
            }

            var userDto = new UserDto
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                Role = user.Role?.Name,
                IsVerified = user.IsVerified,
                IsDeleted = user.IsDeleted,
                Created_At = user.Created_At
            };

            return Ok(userDto); // 200 OK
        }

        // Removed UpdateMyUsername endpoint (moved to AccountApiController)
        // Removed UpdateMyPassword endpoint (moved to AccountApiController)
        // Removed SoftDeleteMyAccount endpoint (moved to AccountApiController)

        /// <summary>
        /// Admin action: Verifies a user's account.
        /// Accessible only by Admin role.
        /// </summary>
        /// <param name="id">The ID of the user to verify.</param>
        /// <returns>200 OK on success, 404 Not Found, or 400 Bad Request.</returns>
        [HttpPut("{id}/verify")] // Route: /api/users/{id}/verify
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> VerifyUser(int id)
        {
            var userToVerify = await _context.User.FindAsync(id);
            if (userToVerify == null)
            {
                return NotFound("User not found."); // 404 Not Found
            }

            if (userToVerify.IsVerified)
            {
                return BadRequest("User is already verified."); // 400 Bad Request
            }

            userToVerify.IsVerified = true;

            try
            {
                await _context.SaveChangesAsync();

                // Send account verified email using template
                try
                {
                    var placeholders = new Dictionary<string, string>
                    {
                        { "UserName", userToVerify.Name },
                        { "CurrentYear", DateTime.UtcNow.Year.ToString() }
                    };
                    var emailBody = await EmailTemplateHelper.LoadTemplateAndPopulateAsync("AccountVerified.html", placeholders);
                    await _emailService.SendEmailAsync(userToVerify.Email, "Your StackFlow Account Has Been Verified", emailBody);
                }
                catch (Exception ex)
                {
                    // Log error, but don't prevent verification
                    Console.WriteLine($"Error sending account verification email: {ex.Message}");
                }

                return Ok(new { message = $"User '{userToVerify.Name}' has been verified successfully." });
            }
            catch (Exception ex)
            {
                // Log the exception
                return StatusCode(500, $"Error verifying user: {ex.Message}"); // 500 Internal Server Error
            }
        }

        /// <summary>
        /// Admin action: Updates a user's role.
        /// Accessible only by Admin role.
        /// </summary>
        /// <param name="id">The ID of the user whose role is to be updated.</param>
        /// <param name="dto">The new role ID.
        /// </param>
        /// <returns>200 OK on success, 404 Not Found, 400 Bad Request, or 403 Forbidden.</returns>
        [HttpPut("{id}/role")] // Route: /api/users/{id}/role
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateUserRole(int id, [FromBody] UpdateUserRoleDto dto)
        {
            var userToUpdate = await _context.User.Include(u => u.Role).FirstOrDefaultAsync(u => u.Id == id);
            if (userToUpdate == null)
            {
                return NotFound("User not found."); // 404 Not Found
            }

            var newRole = await _context.Role.FindAsync(dto.NewRoleId);
            if (newRole == null)
            {
                return BadRequest("New role not found."); // 400 Bad Request
            }

            var currentAdminIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (int.TryParse(currentAdminIdString, out int currentAdminId) && currentAdminId == id)
            {
                return Forbid("You cannot change your own role via this endpoint. Use the 'Manage Account' section for self-management."); // 403 Forbidden
            }

            userToUpdate.Role_Id = newRole.Id;

            try
            {
                await _context.SaveChangesAsync();
                return Ok(new { message = $"User '{userToUpdate.Name}' role updated to '{newRole.Name}' successfully." });
            }
            catch (Exception ex)
            {
                // Log the exception
                return StatusCode(500, $"Error updating user role: {ex.Message}"); // 500 Internal Server Error
            }
        }

        /// <summary>
        /// Admin action: deletes a user's account by setting IsDeleted to true.
        /// Their assigned tickets will be reassigned to the deleting admin.
        /// Accessible only by Admin role.
        /// </summary>
        /// <param name="id">The ID of the user to soft-delete.</param>
        /// <returns>200 OK on success, 404 Not Found, 400 Bad Request, or 403 Forbidden.</returns>
        [HttpDelete("{id}")] // Route: /api/users/{id}
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var userToDelete = await _context.User
                                             .Include(u => u.AssignedTickets) // Include assigned tickets for reassignment
                                             .FirstOrDefaultAsync(u => u.Id == id);

            if (userToDelete == null)
            {
                return NotFound("User not found."); // 404 Not Found
            }

            if (userToDelete.IsDeleted)
            {
                return BadRequest("User is already deleted."); // 400 Bad Request
            }

            var currentAdminIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(currentAdminIdString, out int currentAdminId))
            {
                return Unauthorized("Admin ID not found in claims."); // Should not happen with [Authorize]
            }

            if (currentAdminId == id)
            {
                return Forbid("You cannot soft-delete your own account via this endpoint. Use /api/account/me for self-deletion."); // 403 Forbidden
            }

            try
            {
                var adminRole = await _context.Role.FirstOrDefaultAsync(r => r.Name == "Admin");
                if (adminRole == null)
                {
                    return StatusCode(500, "Admin role not found for ticket reassignment.");
                }

                var adminUser = await _context.User
                                              .Where(u => u.Role_Id == adminRole.Id && !u.IsDeleted && u.IsVerified)
                                              .FirstOrDefaultAsync();

                if (adminUser == null)
                {
                    return StatusCode(500, "No active admin found to reassign tickets. Cannot soft-delete user."); // 500 Internal Server Error
                }

                // Reassign tickets from the user being deleted to the admin
                foreach (var ticket in userToDelete.AssignedTickets)
                {
                    ticket.Assigned_To = adminUser.Id;
                }
                await _context.SaveChangesAsync(); // Save ticket reassignments first

                userToDelete.IsDeleted = true;
                await _context.SaveChangesAsync(); // Then save user deletion status

                // Send account deleted email to the user using template
                try
                {
                     var placeholders = new Dictionary<string, string>
                     {
                         { "UserName", userToDelete.Name },
                         { "CurrentYear", DateTime.UtcNow.Year.ToString() }
                     };
                     var emailBody = await EmailTemplateHelper.LoadTemplateAndPopulateAsync("AccountDeleted.html", placeholders);
                     await _emailService.SendEmailAsync(userToDelete.Email, "Your StackFlow Account Has Been Deleted", emailBody);
                }
                catch (Exception ex)
                {
                    // Log error, but don't prevent deletion
                    Console.WriteLine($"Error sending account deletion email to user: {ex.Message}");
                }

                // Optional: Send email to admin about ticket reassignment using template
                if (adminUser != null) // Ensure adminUser was found earlier
                {
                    try
                    {
                         var adminPlaceholders = new Dictionary<string, string>
                         {
                             { "DeletedUserName", userToDelete.Name },
                             { "AdminUserName", adminUser.Name },
                             { "CurrentYear", DateTime.UtcNow.Year.ToString() }
                         };
                          var adminEmailBody = await EmailTemplateHelper.LoadTemplateAndPopulateAsync("AdminTicketReassignment.html", adminPlaceholders);
                         await _emailService.SendEmailAsync(adminUser.Email, "User Account Deleted and Tickets Reassigned", adminEmailBody);
                    }
                    catch (Exception ex)
                    {
                        // Log error, but don't prevent deletion or user email
                        Console.WriteLine($"Error sending admin notification email about ticket reassignment: {ex.Message}");
                    }
                }

                return Ok(new { message = $"User '{userToDelete.Name}' soft-deleted and their tickets reassigned to {adminUser.Name}." });
            }
            catch (Exception ex)
            {
                // Log the exception
                return StatusCode(500, $"Error soft-deleting user: {ex.Message}"); // 500 Internal Server Error
            }
        }

    }
}
