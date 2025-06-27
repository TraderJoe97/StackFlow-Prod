using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StackFlow.Data;
using Microsoft.AspNetCore.Mvc.Rendering;
using StackFlow.Models;
using System.Linq;
using System.Collections.Generic; // For List<string> errors
using System.Threading.Tasks;

// Removed: using Microsoft.Extensions.Logging;

namespace StackFlow.Controllers
{
    public class AdminController : Controller
    {
        private readonly AppDbContext _context;
        // Removed: private readonly ILogger<AdminController> _logger;

        public AdminController(AppDbContext context) // Removed ILogger from constructor
        {
            _context = context;
            // Removed: _logger = logger;
        }

        // Action to view user details
        public async Task<IActionResult> ViewUser(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                TempData["ErrorMessage"] = "Error: User ID was not provided to view details.";
                return RedirectToAction(nameof(Index), "UserReports");
            }

            if (!int.TryParse(id, out int userId))
            {
                TempData["ErrorMessage"] = $"Error: Invalid user ID format '{id}'. Please provide a valid number.";
                return RedirectToAction(nameof(Index), "UserReports");
            }

            var user = await _context.User.Include(u => u.Role)
                .FirstOrDefaultAsync(m => m.Id == userId);

            if (user == null)
            {
                TempData["ErrorMessage"] = $"Error: User with ID '{id}' not found.";
                return RedirectToAction(nameof(Index), "UserReports");
            }
            return View(user);
        }

        // Action to edit user details (GET)
        public async Task<IActionResult> EditUser(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                TempData["ErrorMessage"] = "Error: User ID was not provided to edit.";
                return RedirectToAction(nameof(Index), "UserReports");
            }

            if (!int.TryParse(id, out int userId))
            {
                TempData["ErrorMessage"] = $"Error: Invalid user ID format '{id}'. Cannot retrieve user for editing.";
                return RedirectToAction(nameof(Index), "UserReports");
            }

            var user = await _context.User.FindAsync(userId);
            if (user == null)
            {
                TempData["ErrorMessage"] = $"Error: User with ID '{id}' not found for editing.";
                return RedirectToAction(nameof(Index), "UserReports");
            }

            await PopulateRolesDropdown(user.Role_Id);
            return View(user);
        }

        // Action to edit user details (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        // Move [Bind] from here...
        public async Task<IActionResult> EditUser(string id, [Bind("Id,Name,Email,Role_Id,IsActive")] User user) // ...to HERE, directly on the 'user' parameter
        {
            if (string.IsNullOrEmpty(id))
            {
                TempData["ErrorMessage"] = "Error: User ID was not provided for the update operation.";
                return RedirectToAction(nameof(Index), "UserReports");
            }

            if (!int.TryParse(id, out int userId))
            {
                TempData["ErrorMessage"] = $"Error: Invalid user ID format '{id}'. Cannot proceed with update.";
                return RedirectToAction(nameof(Index), "UserReports");
            }

            // Ensure the ID from the route matches the ID from the form
            if (userId != user.Id)
            {
                TempData["ErrorMessage"] = "Security Error: Mismatch between user ID in URL and form data. Please try again.";
                return NotFound();
            }
          try
                {
                    var userToUpdate = await _context.User.FirstOrDefaultAsync(u => u.Id == userId);

                    if (userToUpdate == null)
                    {
                        TempData["ErrorMessage"] = $"Error: User with ID '{id}' not found for update. It might have been deleted.";
                        return NotFound();
                    }

                    userToUpdate.Name = user.Name;
                    userToUpdate.Email = user.Email;
                    userToUpdate.Role_Id = user.Role_Id;
                    userToUpdate.IsActive = user.IsActive;

                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = $"User '{userToUpdate.Name}' (ID: {userToUpdate.Id}) updated successfully!";
                    return RedirectToAction(nameof(Index), "UserReports");
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    if (!UserExists(user.Id))
                    {
                        TempData["ErrorMessage"] = $"Concurrency Error: User '{user.Name}' no longer exists. It may have been deleted by another user.";
                        return NotFound();
                    }
                    else
                    {
                        TempData["ErrorMessage"] = $"Concurrency Error: User '{user.Name}' was modified by another administrator while you were editing. Please review the changes and try again.";
                    }
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = $"An unexpected error occurred while saving changes for user '{user.Name}': {ex.Message}";
                }
           

            await PopulateRolesDropdown(user.Role_Id);
            return View(user);
        }

        // Action to delete a user (GET)
        public async Task<IActionResult> DeleteUser(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                TempData["ErrorMessage"] = "Error: User ID was not provided for deletion confirmation.";
                return RedirectToAction(nameof(Index), "UserReports");
            }

            if (!int.TryParse(id, out int userId))
            {
                TempData["ErrorMessage"] = $"Error: Invalid user ID format '{id}'. Cannot retrieve user for deletion confirmation.";
                return RedirectToAction(nameof(Index), "UserReports");
            }

            var user = await _context.User.Include(u => u.Role)
                .FirstOrDefaultAsync(m => m.Id == userId);
            if (user == null)
            {
                TempData["ErrorMessage"] = $"Error: User with ID '{id}' not found for deletion confirmation.";
                return RedirectToAction(nameof(Index), "UserReports");
            }
            return View(user);
        }

        // Action to delete a user (POST - Deactivate)
        [HttpPost, ActionName("DeleteUser")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                TempData["ErrorMessage"] = "Error: User ID was not provided for account deactivation.";
                return RedirectToAction(nameof(Index), "UserReports");
            }

            if (!int.TryParse(id, out int userId))
            {
                TempData["ErrorMessage"] = $"Error: Invalid user ID format '{id}'. Cannot proceed with deactivation.";
                return RedirectToAction(nameof(Index), "UserReports");
            }

            var user = await _context.User.FindAsync(userId);
            if (user != null)
            {
                try
                {
                    if (!user.IsActive)
                    {
                        TempData["ErrorMessage"] = $"User '{user.Name}' (ID: {user.Id}) is already deactivated.";
                        return RedirectToAction(nameof(Index), "UserReports");
                    }

                    user.IsActive = false;
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = $"User '{user.Name}' (ID: {user.Id}) has been successfully deactivated.";
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = $"An unexpected error occurred while deactivating user '{user.Name}': {ex.Message}";
                }
            }
            else
            {
                TempData["ErrorMessage"] = $"Error: User with ID '{id}' not found for deactivation. It might have been deleted already.";
            }
            return RedirectToAction(nameof(Index), "UserReports");
        }

        // Action to reactivate a user
        public async Task<IActionResult> ReactivateUser(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                TempData["ErrorMessage"] = "Error: User ID was not provided for account reactivation.";
                return RedirectToAction(nameof(Index), "UserReports");
            }

            if (!int.TryParse(id, out int userId))
            {
                TempData["ErrorMessage"] = $"Error: Invalid user ID format '{id}'. Cannot proceed with reactivation.";
                return RedirectToAction(nameof(Index), "UserReports");
            }

            var user = await _context.User.FindAsync(userId);
            if (user != null)
            {
                try
                {
                    if (user.IsActive)
                    {
                        TempData["ErrorMessage"] = $"User '{user.Name}' (ID: {user.Id}) is already active.";
                        return RedirectToAction(nameof(Index), "UserReports");
                    }

                    user.IsActive = true;
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = $"User '{user.Name}' (ID: {user.Id}) has been successfully reactivated.";
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = $"An unexpected error occurred while reactivating user '{user.Name}': {ex.Message}";
                }
            }
            else
            {
                TempData["ErrorMessage"] = $"Error: User with ID '{id}' not found for reactivation. It might have been deleted.";
            }
            return RedirectToAction(nameof(Index), "UserReports");
        }

        private bool UserExists(int id)
        {
            return _context.User.Any(e => e.Id == id);
        }

        // Helper method to populate roles dropdown
        private async Task PopulateRolesDropdown(int? selectedRoleId = null)
        {
            var roles = await _context.Role.ToListAsync();
            ViewBag.Roles = roles.Select(r => new SelectListItem
            {
                Value = r.Id.ToString(),
                Text = r.Name,
                Selected = (selectedRoleId.HasValue && selectedRoleId.Value == r.Id)
            }).ToList();
        }
    }
}