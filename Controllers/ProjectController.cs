using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using StackFlow.Data;
using StackFlow.Models;
using StackFlow.Hubs;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace StackFlow.Controllers
{
    [Authorize] // All actions in this controller require authentication
    public class ProjectController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IHubContext<DashboardHub> _hubContext;

        public ProjectController(AppDbContext context, IHubContext<DashboardHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }

        // <summary>
        /// Displays a list of all projects.
        /// Accessible to all authenticated users.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var projects = await _context.Project
                                        .Include(p => p.CreatedBy)
                                        .OrderByDescending(p => p.Start_Date) // Order by start date for logical display
                                        .ToListAsync();
            return View(projects);
        }

        /// <summary>
        /// Displays the form for creating a new project.
        /// Accessible only to Admin users.
        /// </summary>
        
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public IActionResult CreateProject()
        {
            ViewBag.ProjectStatuses = new SelectList(new List<string> { "Active", "Completed", "On_Hold" });
            return View(new Project());
        }

        /// <summary>
        /// Handles the submission of the new project creation form.
        /// Accessible only to Admin users.
        /// </summary>
        /// <param name="project">The Project model populated from the form.</param>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateProject([Bind("Name,Description,Start_Date,Due_Date,Status")] Project project)
        {
            ModelState.Remove("CreatedBy");
            ModelState.Remove("Tickets");
            ModelState.Remove("CreatedByUserId");

            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdString, out int currentUserId))
            {
                TempData["ErrorMessage"] = "Authentication error: Could not identify user.";
                return RedirectToAction("Login", "Account");
            }

            project.Created_By = currentUserId;

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Add(project);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = $"Project '{project.Name}' created successfully!";

                    // Send minimal data: action and project ID
                    await _hubContext.Clients.All.SendAsync("ReceiveProjectUpdate", "created", project.Id);

                    return RedirectToAction("Index", "Dashboard"); // Redirect to Dashboard Index
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = $"Error creating project: {ex.Message}";
                    ViewBag.ProjectStatuses = new SelectList(new List<string> { "Active", "Completed", "On_Hold" }, project.Status);
                    return View(project);
                }
            }
            TempData["ErrorMessage"] = "Please correct the errors in the form.";
            ViewBag.ProjectStatuses = new SelectList(new List<string> { "Active", "Completed", "On_Hold" }, project.Status);

            foreach (var modelStateEntry in ModelState.Values)
            {
                foreach (var error in modelStateEntry.Errors)
                {
                    Console.WriteLine($"Validation Error (CreateProject): {error.ErrorMessage}");
                }
            }
            return View(project);
        }

        /// <summary>
        /// Displays the detailed view of a single project, including its associated tickets.
        /// Accessible to all authenticated users.
        /// </summary>
        /// <param name="id">The ID of the project to view.</param>
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> ProjectDetails(int id)
        {
            var project = await _context.Project
                                        .Include(p => p.CreatedBy)
                                        .Include(p => p.Tickets)
                                        .FirstOrDefaultAsync(m => m.Id == id);

            if (project == null)
            {
                return NotFound();
            }

            return View(project);
        }

        /// <summary>
        /// Displays the form for editing an existing project.
        /// Accessible only to Admin users.
        /// </summary>
        /// <param name="id">The ID of the project to edit.</param>
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> EditProject(int id)
        {
            var project = await _context.Project.FindAsync(id);
            if (project == null)
            {
                return NotFound();
            }

            ViewBag.ProjectStatuses = new SelectList(new List<string> { "Active", "Completed", "On_Hold" }, project.Status);
            return View(project);
        }

        /// <summary>
        /// Handles the submission of the edited project form.
        /// Accessible only to Admin users.
        /// </summary>
        /// <param name="id">The ID of the project being edited.</param>
        /// <param name="project">The Project model populated from the form.</param>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> EditProject(int id, [Bind("Id,Name,Description,Start_Date,Due_Date,Status")] Project project)
        {
            if (id != project.Id)
            {
                return NotFound();
            }

            // Remove navigation properties from ModelState validation, as they are not posted from the form
            ModelState.Remove("CreatedBy");
            ModelState.Remove("Tickets");
            ModelState.Remove("CreatedByUserId"); // This will be preserved from the original entity

            if (ModelState.IsValid)
            {
                try
                {
                    // Fetch the original project to preserve CreatedByUserId
                    var originalProject = await _context.Project.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);
                    if (originalProject == null)
                    {
                        return NotFound();
                    }
                    project.Created_By = originalProject.Created_By; // Preserve the creator

                    _context.Update(project);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = $"Project '{project.Name}' updated successfully!";

                    // Send minimal data: action and project ID
                    await _hubContext.Clients.All.SendAsync("ReceiveProjectUpdate", "updated", project.Id);
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await _context.Project.AnyAsync(e => e.Id == project.Id))
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
                    TempData["ErrorMessage"] = $"Error updating project: {ex.Message}";
                    ViewBag.ProjectStatuses = new SelectList(new List<string> { "Active", "Completed", "On_Hold" }, project.Status);
                    return View(project);
                }
                return RedirectToAction("ProjectDetails", new { id = project.Id }); // Redirect to project details
            }

            TempData["ErrorMessage"] = "Please correct the errors in the form.";
            ViewBag.ProjectStatuses = new SelectList(new List<string> { "Active", "Completed", "On_Hold" }, project.Status);

            foreach (var modelStateEntry in ModelState.Values)
            {
                foreach (var error in modelStateEntry.Errors)
                {
                    Console.WriteLine($"Validation Error (EditProject): {error.ErrorMessage}");
                }
            }
            return View(project);
        }


        /// <summary>
        /// POST action to delete a project and its associated tickets.
        /// Accessible only to Admin users.
        /// </summary>
        /// <param name="id">The ID of the project to delete.</param>
        [HttpPost, ActionName("DeleteProject")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteProjectConfirmed(int id)
        {
            var project = await _context.Project
                                        .Include(p => p.Tickets)
                                        .FirstOrDefaultAsync(p => p.Id == id);

            if (project == null)
            {
                TempData["ErrorMessage"] = "Project not found.";
                return NotFound();
            }

            string projectName = project.Name;
            int ticketCount = project.Tickets.Count;

            try
            {
                // Remove associated tickets first if not configured for cascade delete in DB
                _context.Ticket.RemoveRange(project.Tickets);
                _context.Project.Remove(project);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Project '{projectName}' and its {ticketCount} associated tickets deleted successfully.";
                // Send minimal data: action and project ID
                await _hubContext.Clients.All.SendAsync("ReceiveProjectUpdate", "deleted", id);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error deleting project: {ex.Message}";
            }
            return RedirectToAction("Index", "Dashboard"); // Redirect to Dashboard Index
        }
    }
}
