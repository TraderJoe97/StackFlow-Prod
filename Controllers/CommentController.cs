
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.AspNetCore.Mvc.Rendering;
//using Microsoft.EntityFrameworkCore;
//using StackFlow.Data;
//using StackFlow.Models;

//namespace StackFlow.Controllers
//{
//    public class CommentController : Controller
//    {
//        private readonly AppDbContext _context;

//        public CommentController(AppDbContext context)
//        {
//            _context = context;
//        }

//        // GET: Comment
//        public async Task<IActionResult> Index()
//        {
//            var comments = _context.Comments
//                                   .Include(c => c.Ticket)
//                                   .Include(c => c.CreatedBy);
//            return View(await comments.ToListAsync());
//        }

//        // GET: Comment/Details/5
//        public async Task<IActionResult> Details(int? id)
//        {
//            if (id == null) return NotFound();

//            var comment = await _context.Comments
//                                        .Include(c => c.Ticket)
//                                        .Include(c => c.CreatedBy)
//                                        .FirstOrDefaultAsync(m => m.Id == id);
//            if (comment == null) return NotFound();

//            return View(comment);
//        }

//        // GET: Comment/Create
//        public IActionResult Create()
//        {
//            ViewData["Ticket_Id"] = new SelectList(_context.Tickets, "Id", "Title");
//            ViewData["Created_By"] = new SelectList(_context.Users, "Id", "Name");
//            return View();
//        }

//        // POST: Comment/Create
//        [HttpPost]
//        [ValidateAntiForgeryToken]
//        public async Task<IActionResult> Create([Bind("Ticket_Id,Content,Created_By,Created_At")] Comment comment)
//        {
//            if (ModelState.IsValid)
//            {
//                _context.Add(comment);
//                await _context.SaveChangesAsync();
//                return RedirectToAction(nameof(Index));
//            }

//            ViewData["Ticket_Id"] = new SelectList(_context.Tickets, "Id", "Title", comment.Ticket_Id);
//            ViewData["Created_By"] = new SelectList(_context.Users, "Id", "Name", comment.Created_By);
//            return View(comment);
//        }

//        // GET: Comment/Edit/5
//        public async Task<IActionResult> Edit(int? id)
//        {
//            if (id == null) return NotFound();

//            var comment = await _context.Comments.FindAsync(id);
//            if (comment == null) return NotFound();

//            ViewData["Ticket_Id"] = new SelectList(_context.Tickets, "Id", "Title", comment.Ticket_Id);
//            ViewData["Created_By"] = new SelectList(_context.Users, "Id", "Name", comment.Created_By);
//            return View(comment);
//        }

//        // POST: Comment/Edit/5
//        [HttpPost]
//        [ValidateAntiForgeryToken]
//        public async Task<IActionResult> Edit(int id, [Bind("Id,Ticket_Id,Content,Created_By,Created_At")] Comment comment)
//        {
//            if (id != comment.Id) return NotFound();

//            if (ModelState.IsValid)
//            {
//                try
//                {
//                    _context.Update(comment);
//                    await _context.SaveChangesAsync();
//                }
//                catch (DbUpdateConcurrencyException)
//                {
//                    if (!CommentExists(comment.Id))
//                        return NotFound();
//                    else
//                        throw;
//                }
//                return RedirectToAction(nameof(Index));
//            }

//            ViewData["Ticket_Id"] = new SelectList(_context.Tickets, "Id", "Title", comment.Ticket_Id);
//            ViewData["Created_By"] = new SelectList(_context.Users, "Id", "Name", comment.Created_By);
//            return View(comment);
//        }

//        // GET: Comment/Delete/5
//        public async Task<IActionResult> Delete(int? id)
//        {
//            if (id == null) return NotFound();

//            var comment = await _context.Comments
//                                        .Include(c => c.Ticket)
//                                        .Include(c => c.CreatedBy)
//                                        .FirstOrDefaultAsync(m => m.Id == id);
//            if (comment == null) return NotFound();

//            return View(comment);
//        }

//        // POST: Comment/Delete/5
//        [HttpPost, ActionName("Delete")]
//        [ValidateAntiForgeryToken]
//        public async Task<IActionResult> DeleteConfirmed(int id)
//        {
//            var comment = await _context.Comments.FindAsync(id);
//            _context.Comments.Remove(comment);
//            await _context.SaveChangesAsync();
//            return RedirectToAction(nameof(Index));
//        }

//        private bool CommentExists(int id)
//        {
//            return _context.Comments.Any(e => e.Id == id);
//        }
//    }
//}

