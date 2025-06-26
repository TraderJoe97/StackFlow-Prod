using Microsoft.AspNetCore.Mvc;
using StackFlow.Data;
using StackFlow.Models;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Rendering; // Needed for SelectList

namespace StackFlow.Controllers
{
    public class ProjectController : Controller
    {
        private readonly AppDbContext _context;

        public ProjectController(AppDbContext context)
        {
            _context = context;
        }

        // GET: Project
        public async Task<IActionResult> Index()
        {
            // Retrieve all projects from the database and pass them to the view.
            return View(await _context.Project.ToListAsync());
        }

        // GET: Project/Details/5
        public async Task<IActionResult> Details(int? id) // Nullable int for optional ID
        {
            if (id == null)
            {
                return NotFound(); // Return 404 if no ID is provided
            }

            // Find the project by ID. Use SingleOrDefaultAsync to handle cases where no project is found.
            var project = await _context.Project
                                        .FirstOrDefaultAsync(m => m.Id == id);
            if (project == null)
            {
                return NotFound(); // Return 404 if project not found
            }

            return View(project);
        }

        // GET: Project/Create
        public IActionResult Create()
        {
            // Populate the ViewBag with status options for the dropdown
            ViewBag.ProjectStatuses = Enum.GetNames(typeof(ProjectStatus))
                                          .Select(name => new SelectListItem
                                          {
                                              Value = name,
                                              Text = name.Replace("_", " ") // Make "On_Hold" display as "On Hold"
                                          }).ToList();
            return View();
        }

        // POST: Project/Create
        [HttpPost]
        [ValidateAntiForgeryToken] // Protects against Cross-Site Request Forgery attacks
        public async Task<IActionResult> Create([Bind("Name,Description,Start_Date,Due_Date,Status")] Project project)
        {
            // This is where you would typically get the ID of the logged-in user.
            // For now, we'll use a placeholder or assume it's set elsewhere.
            // In a real application, you'd use Identity or similar for authentication.
            project.Created_By = 1; // **IMPORTANT: Replace with actual logged-in User ID**

            if (ModelState.IsValid) // Check if the model state is valid based on annotations
            {
                // Add the new project to the database context
                _context.Add(project);
                // Save changes asynchronously
                await _context.SaveChangesAsync();
                // Redirect to the Index action to show the updated list
                return RedirectToAction(nameof(Index));
            }

            // If model state is invalid, re-populate status options and return the view with validation errors
            ViewBag.ProjectStatuses = Enum.GetNames(typeof(ProjectStatus))
                                          .Select(name => new SelectListItem
                                          {
                                              Value = name,
                                              Text = name.Replace("_", " ")
                                          }).ToList();
            return View(project);
        }

        // GET: Project/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var project = await _context.Project.FindAsync(id);
            if (project == null)
            {
                return NotFound();
            }

            // Populate the ViewBag with status options for the dropdown
            ViewBag.ProjectStatuses = Enum.GetNames(typeof(ProjectStatus))
                                          .Select(name => new SelectListItem
                                          {
                                              Value = name,
                                              Text = name.Replace("_", " "),
                                              Selected = (name == project.Status) // Select the current status
                                          }).ToList();

            return View(project);
        }

        // POST: Project/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Description,Start_Date,Due_Date,Status,Created_By")] Project project)
        {
            if (id != project.Id)
            {
                return NotFound(); // Mismatch between route ID and model ID
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Update the project in the database context
                    _context.Update(project);
                    // Save changes asynchronously
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    // Handle concurrency conflicts if the project was modified by another user
                    if (!_context.Project.Any(e => e.Id == project.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw; // Re-throw if it's another type of exception
                    }
                }
                return RedirectToAction(nameof(Index)); // Redirect to Index after successful update
            }

            // If model state is invalid, re-populate status options and return the view with validation errors
            ViewBag.ProjectStatuses = Enum.GetNames(typeof(ProjectStatus))
                                          .Select(name => new SelectListItem
                                          {
                                              Value = name,
                                              Text = name.Replace("_", " "),
                                              Selected = (name == project.Status)
                                          }).ToList();
            return View(project);
        }

        // GET: Project/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var project = await _context.Project
                                        .FirstOrDefaultAsync(m => m.Id == id);
            if (project == null)
            {
                return NotFound();
            }

            return View(project);
        }

        // POST: Project/Delete/5
        [HttpPost, ActionName("Delete")] // ActionName specifies the action to be invoked by the form
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            // Find the project to delete
            var project = await _context.Project.FindAsync(id);
            if (project != null) // Check if the project exists before attempting to remove
            {
                _context.Project.Remove(project); // Remove the project from the context
                await _context.SaveChangesAsync(); // Save changes
            }
            return RedirectToAction(nameof(Index)); // Redirect to Index
        }
    }
}
