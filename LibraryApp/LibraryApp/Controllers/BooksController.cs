using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LibraryApp.Data;
using LibraryApp.Models;

namespace LibraryApp.Controllers
{
    public class BooksController : Controller
    {
        private readonly ApplicationDbContext _context;

        public BooksController(ApplicationDbContext context)
        {
            _context = context;
        }
        
        public async Task<IActionResult> Index(int page = 1)
        {
            int pageSize = 2; 
            
            int totalBooks = await _context.Books.CountAsync();
            
            var books = await _context.Books
                .OrderByDescending(b => b.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
            
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling(totalBooks / (double)pageSize);

            return View(books);
        }
        
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var book = await _context.Books.FirstOrDefaultAsync(m => m.Id == id);
            if (book == null) return NotFound();

            return View(book);
        }
        
        public IActionResult Create()
        {
            return View();
        }
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Book book)
        {
            if (ModelState.IsValid)
            {
                book.CreatedAt = DateTime.UtcNow;
                book.IsAvailable = true; 
                _context.Add(book);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(book);
        }
        
   
        [HttpGet]
        public async Task<IActionResult> Take(int? id)
        {
            if (id == null) return NotFound();

            var book = await _context.Books.FirstOrDefaultAsync(m => m.Id == id);
            if (book == null || !book.IsAvailable) 
            {
                return RedirectToAction(nameof(Index));
            }

            return View(book);
        }
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Take(int id, string email)
        {
            var book = await _context.Books.FindAsync(id);
            if (book == null || !book.IsAvailable)
            {
                return NotFound();
            }
            
            var user = await _context.Users
                .Include(u => u.BorrowRecords)
                .FirstOrDefaultAsync(u => u.Email == email);

            if (user == null)
            {
                ModelState.AddModelError("", "Пользователь с таким email не найден в системе.");
                return View(book);
            }
            
            int activeBooksCount = user.BorrowRecords.Count(r => r.ReturnDate == null);
            if (activeBooksCount >= 3)
            {
                ModelState.AddModelError("", "Превышен лимит: у вас на руках уже находится 3 книги.");
                return View(book);
            }
            
            book.IsAvailable = false;
            
            var borrowRecord = new BorrowRecord
            {
                UserId = user.Id,
                BookId = book.Id,
                BorrowDate = DateTime.UtcNow
            };

            _context.BorrowRecords.Add(borrowRecord);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }    
    }
}