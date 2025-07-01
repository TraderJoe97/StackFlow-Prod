using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using StackFlow.Data;
using StackFlow.Models;
using StackFlow.ViewModels;
using StackFlow.Hubs;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace StackFlow.Controllers
{
    [Authorize(Roles = "Admin")] // All actions in this controller accessible only to Admin
    public class ReportController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IHubContext<DashboardHub> _hubContext;

        public ReportController(AppDbContext context, IHubContext<DashboardHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }

        /// <summary>
        /// Displays a report of projects with their ticket statistics with pagination.
        /// </summary>
        /// <param name="page">Current page number (defaults to 1).</param>
        /// <param name="pageSize">Number of items per page (defaults to 10).</param>
        [HttpGet]
        public async Task<IActionResult> ProjectReports(int page = 1, int pageSize = 10)
        {
            // Ensure page and pageSize are valid
            page = Math.Max(1, page);
            pageSize = Math.Max(1, pageSize); // Prevent division by zero or negative pageSize

            var projectsQuery = _context.Project
                                         .Include(p => p.Tickets)
                                         .OrderBy(p => p.Name);

            // Get total count for pagination
            var totalProjects = await projectsQuery.CountAsync();

            // Apply pagination
            var projects = await projectsQuery
                                .Skip((page - 1) * pageSize)
                                .Take(pageSize)
                                .ToListAsync();

            var projectReports = projects.Select(p => new ProjectReportViewModel
            {
                Project = p,
                TotalTickets = p.Tickets.Count,
                CompletedTickets = p.Tickets.Count(t => t.Status == "Done"),
                InProgressTickets = p.Tickets.Count(t => t.Status == "In_Progress"),
                ToDoTickets = p.Tickets.Count(t => t.Status == "To_Do"),
                InReviewTickets = p.Tickets.Count(t => t.Status == "In_Review")
            }).ToList();

            // Prepare ViewBag for pagination
            ViewBag.CurrentPage = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalPages = (int)Math.Ceiling(totalProjects / (double)pageSize);
            ViewBag.CurrentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)); // Pass current user ID for UI logic

            return View(projectReports);
        }

        /// <summary>
        /// Displays a report with two tables: active users (with search and pagination)
        /// and pending verification users (with pagination and admin actions).
        /// Excludes soft-deleted users from both tables.
        /// </summary>
        /// <param name="activePage">Current page for active users (defaults to 1).</param>
        /// <param name="activePageSize">Page size for active users (defaults to 10).</param>
        /// <param name="searchQuery">Search query for active users.</param>
        /// <param name="pendingPage">Current page for pending users (defaults to 1).</param>
        /// <param name="pendingPageSize">Page size for pending users (defaults to 10).</param>
        [HttpGet]
        public async Task<IActionResult> UserReports(
            int activePage = 1, int activePageSize = 10, string searchQuery = null,
            int pendingPage = 1, int pendingPageSize = 10)
        {
            // Ensure page and pageSize are valid
            activePage = Math.Max(1, activePage);
            activePageSize = Math.Max(1, activePageSize);
            pendingPage = Math.Max(1, pendingPage);
            pendingPageSize = Math.Max(1, pendingPageSize);

            var model = new UserReportsCombinedViewModel();
            model.ActiveUsersCurrentPage = activePage;
            model.ActiveUsersPageSize = activePageSize;
            model.ActiveUsersSearchQuery = searchQuery;
            model.PendingUsersCurrentPage = pendingPage;
            model.PendingUsersPageSize = pendingPageSize;

            // --- Active Users Query ---
            var activeUsersQuery = _context.User
                                           .Where(u => !u.IsDeleted && u.IsVerified) // Filter for active users
                                           .Include(u => u.AssignedTickets)
                                           .Include(u => u.Role)
                                           .OrderBy(u => u.Name)
                                           .AsQueryable(); // Use AsQueryable for dynamic filtering

            if (!string.IsNullOrWhiteSpace(searchQuery))
            {
                activeUsersQuery = activeUsersQuery.Where(u =>
                    u.Name.Contains(searchQuery) ||
                    u.Email.Contains(searchQuery) ||
                    u.Role.Name.Contains(searchQuery));
            }

            model.ActiveUsersTotalPages = (int)Math.Ceiling(await activeUsersQuery.CountAsync() / (double)activePageSize);
            var activeUsers = await activeUsersQuery
                                        .Skip((activePage - 1) * activePageSize)
                                        .Take(activePageSize)
                                        .ToListAsync();

            model.ActiveUsers = activeUsers.Select(u => new UserReportViewModel
            {
                User = u,
                TotalTicketsAssigned = u.AssignedTickets.Count,
                CompletedTicketsAssigned = u.AssignedTickets.Count(t => t.Status == "Done"),
                InProgressTicketsAssigned = u.AssignedTickets.Count(t => t.Status == "In_Progress"),
                AssignedTickets = u.AssignedTickets.Select(t => new TicketViewModel
                {
                    Id = t.Id,
                    Title = t.Title,
                    Status = t.Status,
                    Priority = t.Priority,
                    DueDate = t.Due_Date
                }).ToList(),
                ToDoTicketsAssigned = u.AssignedTickets.Count(t => t.Status == "To_Do"),
                InReviewTicketsAssigned = u.AssignedTickets.Count(t => t.Status == "In_Review")
            }).ToList();

            // --- Pending Verification Users Query ---
            var pendingUsersQuery = _context.User
                                            .Where(u => !u.IsDeleted && !u.IsVerified) // Filter for pending users
                                            .Include(u => u.AssignedTickets) // Include for potential future use or consistency
                                            .Include(u => u.Role)
                                            .OrderBy(u => u.Created_At) // Order by creation date for pending users
                                            .AsQueryable();

            model.PendingUsersTotalPages = (int)Math.Ceiling(await pendingUsersQuery.CountAsync() / (double)pendingPageSize);
            var pendingUsers = await pendingUsersQuery
                                        .Skip((pendingPage - 1) * pendingPageSize)
                                        .Take(pendingPageSize)
                                        .ToListAsync();

            model.PendingUsers = pendingUsers.Select(u => new UserReportViewModel
            {
                User = u,
                // For pending users, ticket counts might not be as relevant, but keeping for consistency
                TotalTicketsAssigned = u.AssignedTickets.Count,
                CompletedTicketsAssigned = u.AssignedTickets.Count(t => t.Status == "Done"),
                InProgressTicketsAssigned = u.AssignedTickets.Count(t => t.Status == "In_Progress"),
                AssignedTickets = u.AssignedTickets.Select(t => new TicketViewModel
                {
                    Id = t.Id,
                    Title = t.Title,
                    Status = t.Status,
                    Priority = t.Priority,
                    DueDate = t.Due_Date
                }).ToList(),
                ToDoTicketsAssigned = u.AssignedTickets.Count(t => t.Status == "To_Do"),
                InReviewTicketsAssigned = u.AssignedTickets.Count(t => t.Status == "In_Review")
            }).ToList();

            // Fetch all roles for the "Change Role" dropdown (used in active users table)
            ViewBag.Roles = new SelectList(await _context.Role.ToListAsync(), "Id", "Name");
            ViewBag.CurrentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)); // Pass current user ID for UI logic

            return View(model);
        }

        /// <summary>
        /// POST action to update a user's role.
        /// </summary>
        /// <param name="userId">The ID of the user whose role is to be updated.</param>
        /// <param name="newRoleId">The ID of the new role.</param>
        /// <param name="activePage">To redirect back to the correct active users page.</param>
        /// <param name="activePageSize">To redirect back to the correct active users page size.</param>
        /// <param name="searchQuery">To redirect back with the current search query.</param>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateUserRole(int userId, int newRoleId, int activePage, int activePageSize, string searchQuery)
        {
            var userToUpdate = await _context.User.Include(u => u.Role).FirstOrDefaultAsync(u => u.Id == userId);
            if (userToUpdate == null)
            {
                TempData["ErrorMessage"] = "User not found.";
                return NotFound();
            }

            var newRole = await _context.Role.FindAsync(newRoleId);
            if (newRole == null)
            {
                TempData["ErrorMessage"] = "New role not found.";
                return NotFound();
            }

            // Prevent an admin from changing their own role (optional but recommended)
            var currentAdminIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (int.TryParse(currentAdminIdString, out int currentAdminId) && currentAdminId == userId)
            {
                TempData["ErrorMessage"] = "You cannot change your own role.";
                return RedirectToAction(nameof(UserReports), new { activePage, activePageSize, searchQuery });
            }

            string oldRoleTitle = userToUpdate.Role?.Name ?? "Unknown";
            userToUpdate.Role_Id = newRoleId;

            try
            {
                _context.Update(userToUpdate);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"User '{userToUpdate.Name}' role updated from '{oldRoleTitle}' to '{newRole.Name}' successfully.";

                // Fetch user with new role for SignalR payload
                var updatedUser = await _context.User
                                                .Include(u => u.Role)
                                                .FirstOrDefaultAsync(u => u.Id == userToUpdate.Id);

                // Send the updated user object (or DTO) and the action
                await _hubContext.Clients.All.SendAsync("ReceiveUserUpdate", "roleUpdated", updatedUser);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error updating user role: {ex.Message}";
                // Log the exception
            }
            return RedirectToAction(nameof(UserReports), new { activePage, activePageSize, searchQuery });
        }

        /// <summary>
        /// POST action to soft-delete a user.
        /// The user's IsDeleted flag is set to true, and their assigned tickets are reassigned.
        /// </summary>
        /// <param name="id">The ID of the user to soft-delete.</param>
        /// <param name="activePage">To redirect back to the correct active users page.</param>
        /// <param name="activePageSize">To redirect back to the correct active users page size.</param>
        /// <param name="searchQuery">To redirect back with the current search query.</param>
        /// <param name="pendingPage">To redirect back to the correct pending users page.</param>
        /// <param name="pendingPageSize">To redirect back to the correct pending users page size.</param>
        [HttpPost, ActionName("SoftDeleteUser")] // Changed action name
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SoftDeleteUserConfirmed(
            int id, int activePage, int activePageSize, string searchQuery,
            int pendingPage, int pendingPageSize) // Changed method name
        {
            var userToDelete = await _context.User
                                             .Include(u => u.AssignedTickets)
                                             .FirstOrDefaultAsync(u => u.Id == id);

            if (userToDelete == null)
            {
                TempData["ErrorMessage"] = "User not found.";
                return NotFound();
            }

            // Prevent an admin from deleting themselves
            var currentAdminIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (int.TryParse(currentAdminIdString, out int currentAdminId) && currentAdminId == id)
            {
                TempData["ErrorMessage"] = "You cannot delete your own account.";
                return RedirectToAction(nameof(UserReports), new { activePage, activePageSize, searchQuery, pendingPage, pendingPageSize });
            }

            string username = userToDelete.Name;
            int assignedTicketCount = userToDelete.AssignedTickets.Count;

            try
            {
                // Get the current admin user (who is performing the deletion)
                var adminUser = await _context.User.FindAsync(currentAdminId);
                if (adminUser == null)
                {
                    TempData["ErrorMessage"] = "Admin user not found for ticket reassignment.";
                    return RedirectToAction(nameof(UserReports), new { activePage, activePageSize, searchQuery, pendingPage, pendingPageSize });
                }

                // Reassign tickets from the user being deleted to the admin
                foreach (var ticket in userToDelete.AssignedTickets)
                {
                    ticket.Assigned_To = adminUser.Id;
                }
                _context.Ticket.UpdateRange(userToDelete.AssignedTickets);

                // Set IsDeleted to true instead of removing the user
                userToDelete.IsDeleted = true;
                _context.User.Update(userToDelete); // Update the user entity

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"User '{username}' soft-deleted and their tickets reassigned to {adminUser.Name}.";
                await _hubContext.Clients.All.SendAsync("ReceiveUserUpdate", "deleted", new { Id = id, Username = username, AssignedTicketCount = assignedTicketCount }); // SignalR still sends "deleted" for UI refresh
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error soft-deleting user: {ex.Message}";
            }
            return RedirectToAction(nameof(UserReports), new { activePage, activePageSize, searchQuery, pendingPage, pendingPageSize });
        }

        /// <summary>
        /// POST action to verify a user.
        /// Sets the user's IsVerified flag to true.
        /// </summary>
        /// <param name="id">The ID of the user to verify.</param>
        /// <param name="pendingPage">To redirect back to the correct pending users page.</param>
        /// <param name="pendingPageSize">To redirect back to the correct pending users page size.</param>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyUser(int id, int pendingPage, int pendingPageSize)
        {
            var userToVerify = await _context.User.FindAsync(id);
            if (userToVerify == null)
            {
                TempData["ErrorMessage"] = "User not found.";
                return NotFound();
            }

            if (userToVerify.IsVerified)
            {
                TempData["ErrorMessage"] = "User is already verified.";
                return RedirectToAction(nameof(UserReports), new { pendingPage, pendingPageSize });
            }

            userToVerify.IsVerified = true;

            try
            {
                _context.User.Update(userToVerify);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"User '{userToVerify.Name}' has been verified successfully.";

                // Fetch user with new status for SignalR payload
                var verifiedUser = await _context.User
                                                .Include(u => u.Role)
                                                .FirstOrDefaultAsync(u => u.Id == userToVerify.Id);

                await _hubContext.Clients.All.SendAsync("ReceiveUserUpdate", "verified", verifiedUser); // SignalR for UI refresh
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error verifying user: {ex.Message}";
            }
            return RedirectToAction(nameof(UserReports), new { pendingPage, pendingPageSize });
        }
    }
}
