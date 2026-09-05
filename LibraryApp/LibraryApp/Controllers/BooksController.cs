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
        
        public async Task<IActionResult> Index(string searchString, string statusFilter, int? categoryId, string sortOrder, int page = 1)
        {
            int pageSize = 2;

            ViewBag.CurrentSearch = searchString;
            ViewBag.CurrentStatus = statusFilter;
            ViewBag.CurrentCategory = categoryId;
            ViewBag.CurrentSort = sortOrder;
            ViewBag.Categories = await _context.Categories.ToListAsync();

            var booksQuery = _context.Books.Include(b => b.Category).AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                booksQuery = booksQuery.Where(b => b.Title.Contains(searchString) || b.Author.Contains(searchString));
            }

            if (!string.IsNullOrEmpty(statusFilter))
            {
                if (statusFilter == "available")
                    booksQuery = booksQuery.Where(b => b.IsAvailable);
                else if (statusFilter == "borrowed")
                    booksQuery = booksQuery.Where(b => !b.IsAvailable);
            }

            if (categoryId.HasValue && categoryId > 0)
            {
                booksQuery = booksQuery.Where(b => b.CategoryId == categoryId);
            }

            booksQuery = sortOrder switch
            {
                "title_desc" => booksQuery.OrderByDescending(b => b.Title),
                "author" => booksQuery.OrderBy(b => b.Author),
                "author_desc" => booksQuery.OrderByDescending(b => b.Author),
                "status" => booksQuery.OrderBy(b => b.IsAvailable),
                _ => booksQuery.OrderByDescending(b => b.CreatedAt),
            };

            int totalBooks = await booksQuery.CountAsync();

            var books = await booksQuery
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

            var book = await _context.Books
                .Include(b => b.Category)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (book == null) return NotFound();

            return View(book);
        }
        
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            ViewBag.Categories = await _context.Categories.ToListAsync();
            return View();
        }
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Book book, IFormFile? pdfFile)
        {
            if (ModelState.IsValid)
            {
                if (pdfFile != null && pdfFile.Length > 0)
                {
                    string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/pdfs");
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }

                    string uniqueFileName = Guid.NewGuid().ToString() + "_" + pdfFile.FileName;
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await pdfFile.CopyToAsync(fileStream);
                    }

                    book.PdfPath = "/pdfs/" + uniqueFileName;
                }

                book.CreatedAt = DateTime.UtcNow;
                book.IsAvailable = true;
                _context.Add(book);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Categories = await _context.Categories.ToListAsync();
            return View(book);
        }
        
        [HttpGet]
        public async Task<IActionResult> DownloadPdf(int id)
        {
            var book = await _context.Books.FindAsync(id);
            if (book == null || string.IsNullOrEmpty(book.PdfPath))
            {
                return NotFound();
            }

            string filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", book.PdfPath.TrimStart('/'));
            if (!System.IO.File.Exists(filePath))
            {
                return NotFound();
            }

            var memory = new MemoryStream();
            using (var stream = new FileStream(filePath, FileMode.Open))
            {
                await stream.CopyToAsync(memory);
            }
            memory.Position = 0;

            return File(memory, "application/pdf", Path.GetFileName(filePath));
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
        
        public async Task<IActionResult> BorrowedBooks()
        {
            var activeRecords = await _context.BorrowRecords
                .Include(r => r.Book)
                .Include(r => r.User)
                .Where(r => r.ReturnDate == null)
                .ToListAsync();

            return View(activeRecords);
        }
    }
}