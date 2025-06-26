using Microsoft.AspNetCore.Mvc;
using StackFlow.Data;
using StackFlow.Models;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace StackFlow.Controllers
{
	public class ProjectController : Controller
	{
		private readonly AppDbContext _context;

		public ProjectController(AppDbContext context)
		{
			_context = context;
		}

		public async Task<IActionResult> Index()
		{
			return View(await _context.Project.ToListAsync());
		}

		public async Task<IActionResult> Details(int id)
		{
			var project = await _context.Project.FindAsync(id);
			if (project == null) return NotFound();
			return View(project);
		}

		public IActionResult Create() => View();

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Create(Project project)
		{
			if (ModelState.IsValid)
			{
				_context.Add(project);
				await _context.SaveChangesAsync();
				return RedirectToAction(nameof(Index));
			}
			return View(project);
		}

		public async Task<IActionResult> Edit(int id)
		{
			var project = await _context.Project.FindAsync(id);
			if (project == null) return NotFound();
			return View(project);
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Edit(int id, Project project)
		{
			if (id != project.Id) return NotFound();

			if (ModelState.IsValid)
			{
				_context.Update(project);
				await _context.SaveChangesAsync();
				return RedirectToAction(nameof(Index));
			}
			return View(project);
		}

		public async Task<IActionResult> Delete(int id)
		{
			var project = await _context.Project.FindAsync(id);
			if (project == null) return NotFound();
			return View(project);
		}

		[HttpPost, ActionName("Delete")]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> DeleteConfirmed(int id)
		{
			var project = await _context.Project.FindAsync(id);
			_context.Project.Remove(project);
			await _context.SaveChangesAsync();
			return RedirectToAction(nameof(Index));
		}
	}
}
