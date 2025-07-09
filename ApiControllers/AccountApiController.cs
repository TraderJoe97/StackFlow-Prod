using Microsoft.AspNetCore.Mvc;
using StackFlow.Data;
using StackFlow.Models; // Ensure User and Role models are accessible
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using BCrypt.Net; // For password hashing
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt; // For JWT creation
using Microsoft.IdentityModel.Tokens; // For SecurityTokenDescriptor
using System.Text; // For Encoding
using Microsoft.Extensions.Configuration; // For IConfiguration to access JWT settings
using Microsoft.AspNetCore.Authorization; // For [AllowAnonymous]
using Microsoft.AspNetCore.Authentication.JwtBearer; // For JwtBearerDefaults
using StackFlow.ApiControllers.Dtos;

namespace StackFlow.ApiControllers
{
    // This attribute makes this a RESTful API controller.
    // It enables automatic model validation, content negotiation, and other API-specific behaviors.
    [ApiController]
    [Route("api/account")] // Base route for this API controller
    public class AccountApiController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration; // To access JWT settings

        public AccountApiController(AppDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

       

        /// <summary>
        /// API endpoint for user login (JWT Bearer Authentication).
        /// Returns a JWT token upon successful authentication.
        /// </summary>
        [HttpPost("login")] // Route: /api/account/login
        [AllowAnonymous] // Allow unauthenticated users to login to get a token
        public async Task<ActionResult<LoginApiResponse>> Login([FromBody] LoginApiRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest(new { message = "Email and password are required." });
            }

            var user = await _context.User
                                     .Include(u => u.Role)
                                     .FirstOrDefaultAsync(u => u.Email == request.Email);

            if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            {
                return Unauthorized(new { message = "Invalid email or password." });
            }

            if (user.IsDeleted)
            {
                return Unauthorized(new { message = "Account is deleted." });
            }

            if (!user.IsVerified)
            {
                return Unauthorized(new { message = "Account is not verified. Please contact an administrator." });
            }

            // Generate JWT Token
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Name),
                new Claim(ClaimTypes.Email, user.Email)
            };

            if (user.Role != null)
            {
                claims.Add(new Claim(ClaimTypes.Role, user.Role.Name));
            }

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(60), // Token valid for 60 minutes
                signingCredentials: credentials);

            var tokenHandler = new JwtSecurityTokenHandler();
            var tokenString = tokenHandler.WriteToken(token);

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

            return Ok(new LoginApiResponse { Token = tokenString, User = userDto });
        }

        /// <summary>
        /// Registers a new user account via the API.
        /// This endpoint is anonymous, allowing new users to sign up.
        /// </summary>
        /// <param name="dto">The registration data.</param>
        /// <returns>A 201 Created response with the new user's basic info, or 400 Bad Request if validation fails.</returns>
        [HttpPost("register")] // Route: /api/account/register
        [AllowAnonymous] // Allow unauthenticated users to register
        public async Task<ActionResult<UserDto>> Register([FromBody] RegisterUserDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name) || string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Password))
            {
                return BadRequest("All fields are required.");
            }

            if (!dto.Email.EndsWith("@omnitak.com"))
            {
                return BadRequest("Only @omnitak.com email addresses are allowed.");
            }

            if (await _context.User.AnyAsync(u => u.Email == dto.Email))
            {
                return Conflict("Email already registered."); // 409 Conflict
            }
            if (await _context.User.AnyAsync(u => u.Name == dto.Name))
            {
                return Conflict("Username already taken."); // 409 Conflict
            }

            var passwordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);

            var defaultRole = await _context.Role.FirstOrDefaultAsync(r => r.Name == "Developer");
            if (defaultRole == null)
            {
                return StatusCode(500, "Default role 'Developer' not found. Please contact support."); // 500 Internal Server Error
            }

            var newUser = new User
            {
                Name = dto.Name,
                Email = dto.Email,
                PasswordHash = passwordHash,
                Role_Id = defaultRole.Id,
                Created_At = DateTime.UtcNow,
                IsVerified = false, // New users are unverified by default
                IsDeleted = false
            };

            _context.User.Add(newUser);
            await _context.SaveChangesAsync();

            // Return a DTO of the newly created user (without password hash)
            var userDto = new UserDto
            {
                Id = newUser.Id,
                Name = newUser.Name,
                Email = newUser.Email,
                Role = defaultRole.Name,
                IsVerified = newUser.IsVerified,
                IsDeleted = newUser.IsDeleted,
                Created_At = newUser.Created_At
            };

            return CreatedAtAction(nameof(Login), new { email = newUser.Email, password = dto.Password }, userDto); // 201 Created
        }

        

        /// <summary>
        /// Allows an authenticated user to update their own username.
        /// Accessible by any authenticated user via JWT.
        /// </summary>
        /// <param name="dto">The new username.</param>
        /// <returns>200 OK on success, 400 Bad Request, 401 Unauthorized, or 409 Conflict.</returns>
        [HttpPut("me/username")] // Route: /api/account/me/username
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)] // Any authenticated user can access this via JWT
        public async Task<IActionResult> UpdateMyUsername([FromBody] UpdateUsernameDto dto)
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdString, out int currentUserId))
            {
                return Unauthorized("User ID not found in claims."); // 401 Unauthorized
            }

            var userToUpdate = await _context.User.FindAsync(currentUserId);
            if (userToUpdate == null)
            {
                return NotFound("User not found."); // 404 Not Found (shouldn't happen if authenticated)
            }

            if (string.IsNullOrWhiteSpace(dto.NewUsername))
            {
                return BadRequest("New username cannot be empty."); // 400 Bad Request
            }

            if (await _context.User.AnyAsync(u => u.Name == dto.NewUsername && u.Id != currentUserId))
            {
                return Conflict("Username is already taken by another user."); // 409 Conflict
            }

            userToUpdate.Name = dto.NewUsername;

            try
            {
                await _context.SaveChangesAsync();
                return Ok(new { message = "Username updated successfully." }); // 200 OK
            }
            catch (Exception ex)
            {
                // Log the exception
                return StatusCode(500, $"Error updating username: {ex.Message}"); // 500 Internal Server Error
            }
        }

        /// <summary>
        /// Allows an authenticated user to update their own password.
        /// Accessible by any authenticated user via JWT.
        /// </summary>
        /// <param name="dto">The current and new password details.</param>
        /// <returns>200 OK on success, 400 Bad Request, or 401 Unauthorized.</returns>
        [HttpPut("me/password")] // Route: /api/account/me/password
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)] // Any authenticated user can access this via JWT
        public async Task<IActionResult> UpdateMyPassword([FromBody] UpdatePasswordDto dto)
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdString, out int currentUserId))
            {
                return Unauthorized("User ID not found in claims."); // 401 Unauthorized
            }

            var userToUpdate = await _context.User.FindAsync(currentUserId);
            if (userToUpdate == null)
            {
                return NotFound("User not found."); // 404 Not Found
            }

            if (!BCrypt.Net.BCrypt.Verify(dto.CurrentPassword, userToUpdate.PasswordHash))
            {
                return BadRequest("Current password is incorrect."); // 400 Bad Request
            }

            if (string.IsNullOrWhiteSpace(dto.NewPassword) || dto.NewPassword.Length < 6)
            {
                return BadRequest("New password must be at least 6 characters long."); // 400 Bad Request
            }
            if (dto.NewPassword != dto.ConfirmNewPassword)
            {
                return BadRequest("New password and confirmation password do not match."); // 400 Bad Request
            }

            userToUpdate.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);

            try
            {
                await _context.SaveChangesAsync();
                return Ok(new { message = "Password updated successfully." }); // 200 OK
            }
            catch (Exception ex)
            {
                // Log the exception
                return StatusCode(500, $"Error updating password: {ex.Message}"); // 500 Internal Server Error
            }
        }

        /// <summary>
        /// Allows an authenticated user to soft-delete their own account.
        /// Their assigned tickets will be reassigned to an active admin.
        /// Accessible by any authenticated user via JWT.
        /// </summary>
        /// <returns>200 OK on success, 401 Unauthorized, or 500 Internal Server Error.</returns>
        [HttpDelete("me")] // Route: /api/account/me
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)] // Any authenticated user can access this via JWT
        public async Task<IActionResult> SoftDeleteMyAccount()
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdString, out int currentUserId))
            {
                return Unauthorized("User ID not found in claims."); // 401 Unauthorized
            }

            var userToDelete = await _context.User
                                             .Include(u => u.AssignedTickets)
                                             .FirstOrDefaultAsync(u => u.Id == currentUserId);

            if (userToDelete == null)
            {
                return NotFound("User not found."); // 404 Not Found (shouldn't happen if authenticated)
            }

            if (userToDelete.IsDeleted)
            {
                return BadRequest("Account is already deleted."); // 400 Bad Request
            }

            // Prevent an admin from deleting their own account via this endpoint (force them to use admin action if they are the only admin)
            // Or, more robustly, ensure there's at least one other active admin before allowing self-deletion for an admin.
            var adminRole = await _context.Role.FirstOrDefaultAsync(r => r.Name == "Admin");
            if (adminRole != null && userToDelete.Role_Id == adminRole.Id)
            {
                var activeAdmins = await _context.User.CountAsync(u => u.Role_Id == adminRole.Id && !u.IsDeleted && u.IsVerified);
                if (activeAdmins <= 1) // If this admin is the last one
                {
                    return BadRequest("Cannot delete your account as you are the last active administrator. Please ensure there is at least one other active admin before deleting your account.");
                }
            }

            try
            {
                var adminUser = await _context.User
                                              .Where(u => u.Role_Id == adminRole.Id && !u.IsDeleted && u.IsVerified && u.Id != currentUserId)
                                              .FirstOrDefaultAsync();

                if (adminUser == null)
                {
                    return StatusCode(500, "No other active admin found to reassign tickets. Cannot delete account."); // 500 Internal Server Error
                }

                foreach (var ticket in userToDelete.AssignedTickets)
                {
                    ticket.Assigned_To = adminUser.Id;
                }
                await _context.SaveChangesAsync(); // Save ticket reassignments first

                userToDelete.IsDeleted = true;
                await _context.SaveChangesAsync(); // Then save user deletion status

                return Ok(new { message = "Your account has been successfully deleted." }); // 200 OK
            }
            catch (Exception ex)
            {
                // Log the exception
                return StatusCode(500, $"Error deleting account: {ex.Message}"); // 500 Internal Server Error
            }
        }
    }
}
