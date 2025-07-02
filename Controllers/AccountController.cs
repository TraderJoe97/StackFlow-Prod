using Microsoft.AspNetCore.Mvc;
using StackFlow.Data;
using StackFlow.Models;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using BCrypt.Net; // For password hashing
using System;
using System.Collections.Generic;
using System.Linq;
using StackFlow.Utils;

namespace StackFlow.Controllers
{
    public class AccountController : Controller
    {
        private readonly AppDbContext _context;

        public AccountController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Login(string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string email, string password, string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                ViewData["LoginError"] = "Email and password are required.";
                return View();
            }

            // Find user by email
            var user = await _context.User
                                     .Include(u => u.Role)
                                     .FirstOrDefaultAsync(u => u.Email == email);

            if (user == null || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            {
                ViewData["LoginError"] = "Invalid email or password.";
                return View();
            }

            // --- New Verification and Deletion Checks ---
            if (user.IsDeleted)
            {
                // If the user is deleted, redirect to a specific page
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme); // Ensure they are logged out
                return RedirectToAction("DeletedAccount");
            }

            if (!user.IsVerified)
            {
                // If the user is not verified, redirect to a specific page
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme); // Ensure they are logged out
                return RedirectToAction("UnverifiedAccount");
            }
            // --- End New Checks ---

            // Authentication successful, create claims
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()), // Unique identifier for the user
                new Claim(ClaimTypes.Name, user.Name), // Primary name for the user
                new Claim(ClaimTypes.Email, user.Email) // User's email
            };

            // Add role claim if user has a role
            if (user.Role != null)
            {
                claims.Add(new Claim(ClaimTypes.Role, user.Role.Name)); // Add the role title as a role claim
            }

            var claimsIdentity = new ClaimsIdentity(
                claims, CookieAuthenticationDefaults.AuthenticationScheme);

            var authProperties = new AuthenticationProperties
            {
                IsPersistent = true, // Keep session alive across browser restarts
                ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(30) // Set session expiration
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);

            // Redirect to dashboard or returnUrl
            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            else
            {
                return RedirectToAction("Index", "Dashboard"); // Default dashboard
            }
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(string username, string email, string password)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                ViewData["RegistrationError"] = "All fields are required.";
                return View();
            }
            // Basic model validation has passed, now perform custom email validation
            if (!EmailValidator.IsValidEmail(email))
            {
                ModelState.AddModelError("Email", "The email address entered is not in a valid format.");
                ViewData["RegistrationError"] = "The email address entered is not in a valid format.";
                return View(); // Return to view with error
            }

            // Basic email format validation
            if (!email.EndsWith("@omnitak.com"))
            {
                ViewData["RegistrationError"] = "Only @omnitak.com email addresses are allowed.";
                return View();
            }

            // Check if user with this email or username already exists
            if (await _context.User.AnyAsync(u => u.Email == email))
            {
                ViewData["RegistrationError"] = "Email already registered.";
                return View();
            }
            if (await _context.User.AnyAsync(u => u.Name == username))
            {
                ViewData["RegistrationError"] = "Username already taken.";
                return View();
            }

            // Validate Password
            if (!PasswordValidator.IsValidPassword(password))
            {
                // You can be more specific with the error message if you want
                // e.g., "Password must contain only letters, numbers, and @$!%*?& symbols, and be at least 5 characters long."
                ModelState.AddModelError("Password", "Password contains invalid characters or is too short. It must be at least 5 characters long and contain only letters, numbers, and @$!%*?&.");
                ViewData["RegistrationError"] = "Password contains invalid characters or is too short. It must be at least 5 characters long and contain only letters, numbers, and @$!%*?&. symbols";
                return View(); // Return to view with error
            }

            // Hash password
            string passwordHash = BCrypt.Net.BCrypt.HashPassword(password);

            // Assign default role (e.g., 'Developer')
            var defaultRole = await _context.Role.FirstOrDefaultAsync(r => r.Name == "Developer");
            if (defaultRole == null)
            {
                ViewData["RegistrationError"] = "Default role 'Developer' not found. Please contact support.";
                return View();
            }

            var newUser = new User
            {
                Name = username,
                Email = email,
                PasswordHash = passwordHash,
                Role_Id = defaultRole.Id, // Assign the ID of the 'Developer' role
                Created_At = DateTime.UtcNow,
                IsVerified = false, // New users are unverified by default
                IsDeleted = false   // New users are not deleted by default
            };

            _context.User.Add(newUser);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Registration successful! Your account is pending verification by an administrator.";
            return RedirectToAction("Login");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Account");
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }

        // New action for deleted accounts
        [HttpGet]
        public IActionResult DeletedAccount()
        {
            return View();
        }

        // New action for unverified accounts
        [HttpGet]
        public IActionResult UnverifiedAccount()
        {
            return View();
        }

        /// <summary>
        /// Displays the account management page for the currently logged-in user.
        /// </summary>
        [HttpGet]
        [Microsoft.AspNetCore.Authorization.Authorize] // Requires authentication
        public async Task<IActionResult> ManageAccount()
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdString, out int currentUserId))
            {
                TempData["ErrorMessage"] = "User not found. Please log in again.";
                return RedirectToAction("Login");
            }

            var user = await _context.User.FindAsync(currentUserId);
            if (user == null)
            {
                TempData["ErrorMessage"] = "User not found. Please log in again.";
                return RedirectToAction("Login");
            }

            return View(user);
        }

        /// <summary>
        /// Handles updating the username for the currently logged-in user.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Microsoft.AspNetCore.Authorization.Authorize] // Requires authentication
        public async Task<IActionResult> UpdateUsername(int id, string Name)
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdString, out int currentUserId) || currentUserId != id)
            {
                TempData["ErrorMessage"] = "Unauthorized attempt to update username.";
                return Unauthorized();
            }

            var userToUpdate = await _context.User.FindAsync(currentUserId);
            if (userToUpdate == null)
            {
                TempData["ErrorMessage"] = "User not found.";
                return NotFound();
            }

            if (string.IsNullOrWhiteSpace(Name))
            {
                TempData["ErrorMessage"] = "Username cannot be empty.";
                return RedirectToAction(nameof(ManageAccount));
            }

            // Check if the new username is already taken by another user
            if (await _context.User.AnyAsync(u => u.Name == Name && u.Id != currentUserId))
            {
                TempData["ErrorMessage"] = "Username is already taken by another user.";
                return RedirectToAction(nameof(ManageAccount));
            }

            userToUpdate.Name = Name;

            try
            {
                _context.Update(userToUpdate);
                await _context.SaveChangesAsync();

                // Re-sign in the user to update the username claim in their cookie
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, userToUpdate.Id.ToString()),
                    new Claim(ClaimTypes.Name, userToUpdate.Name),
                    new Claim(ClaimTypes.Email, userToUpdate.Email)
                };
                if (userToUpdate.Role != null)
                {
                    claims.Add(new Claim(ClaimTypes.Role, userToUpdate.Role.Name));
                }
                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var authProperties = new AuthenticationProperties { IsPersistent = true, ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(30) };
                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity), authProperties);

                TempData["SuccessMessage"] = "Username updated successfully!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error updating username: {ex.Message}";
            }

            return RedirectToAction(nameof(ManageAccount));
        }

        /// <summary>
        /// Handles updating the password for the currently logged-in user.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Microsoft.AspNetCore.Authorization.Authorize] // Requires authentication
        public async Task<IActionResult> UpdatePassword(int id, string currentPassword, string newPassword, string confirmNewPassword)
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdString, out int currentUserId) || currentUserId != id)
            {
                TempData["ErrorMessage"] = "Unauthorized attempt to update password.";
                return Unauthorized();
            }

            var userToUpdate = await _context.User.FindAsync(currentUserId);
            if (userToUpdate == null)
            {
                TempData["ErrorMessage"] = "User not found.";
                return NotFound();
            }

            // Verify current password
            if (!BCrypt.Net.BCrypt.Verify(currentPassword, userToUpdate.PasswordHash))
            {
                TempData["ErrorMessage"] = "Current password is incorrect.";
                return RedirectToAction(nameof(ManageAccount));
            }

            // Validate new password
            if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 6) // Example: minimum 6 characters
            {
                TempData["ErrorMessage"] = "New password must be at least 6 characters long.";
                return RedirectToAction(nameof(ManageAccount));
            }
            if (newPassword != confirmNewPassword)
            {
                TempData["ErrorMessage"] = "New password and confirmation password do not match.";
                return RedirectToAction(nameof(ManageAccount));
            }

            // Hash and update new password
            userToUpdate.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);

            try
            {
                _context.Update(userToUpdate);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Password updated successfully!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error updating password: {ex.Message}";
            }

            return RedirectToAction(nameof(ManageAccount));
        }

        /// <summary>
        /// Handles soft-deleting the currently logged-in user's account.
        /// Reassigns their tickets to an admin and logs them out.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Microsoft.AspNetCore.Authorization.Authorize] // Requires authentication
        public async Task<IActionResult> DeleteAccount(int id)
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdString, out int currentUserId) || currentUserId != id)
            {
                TempData["ErrorMessage"] = "Unauthorized attempt to delete account.";
                return Unauthorized();
            }

            var userToDelete = await _context.User
                                             .Include(u => u.AssignedTickets)
                                             .FirstOrDefaultAsync(u => u.Id == currentUserId);

            if (userToDelete == null)
            {
                TempData["ErrorMessage"] = "User not found.";
                return NotFound();
            }

            try
            {
                // Find an Admin user to reassign tickets to
                var adminRole = await _context.Role.FirstOrDefaultAsync(r => r.Name == "Admin");
                if (adminRole == null)
                {
                    TempData["ErrorMessage"] = "Admin role not found. Cannot reassign tickets.";
                    return RedirectToAction(nameof(ManageAccount));
                }

                var adminUser = await _context.User.FirstOrDefaultAsync(u => u.Role_Id == adminRole.Id && !u.IsDeleted && u.IsVerified);
                if (adminUser == null)
                {
                    TempData["ErrorMessage"] = "No active admin found to reassign tickets. Account cannot be deleted.";
                    return RedirectToAction(nameof(ManageAccount));
                }

                // Reassign tickets from the user being deleted to the admin
                foreach (var ticket in userToDelete.AssignedTickets)
                {
                    ticket.Assigned_To = adminUser.Id;
                }
                _context.Ticket.UpdateRange(userToDelete.AssignedTickets);

                // Set IsDeleted to true
                userToDelete.IsDeleted = true;
                _context.User.Update(userToDelete);

                await _context.SaveChangesAsync();

                // Sign out the user immediately after deletion
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

                TempData["SuccessMessage"] = "Your account has been successfully deleted.";
                return RedirectToAction("DeletedAccount"); // Redirect to the deleted account page
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error deleting account: {ex.Message}";
                return RedirectToAction(nameof(ManageAccount));
            }
        }
    }
}
