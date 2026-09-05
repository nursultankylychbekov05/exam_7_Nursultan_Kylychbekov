using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LibraryApp.Data;

namespace LibraryApp.Controllers
{
    public class UsersController : Controller
    {
        private readonly ApplicationDbContext _context;

        public UsersController(ApplicationDbContext context)
        {
            _context = context;
        }
        
        [HttpGet]
        public async Task<IActionResult> Account(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                return View(null);
            }

            var user = await _context.Users
                .Include(u => u.BorrowRecords)
                .ThenInclude(r => r.Book)
                .FirstOrDefaultAsync(u => u.Email == email);

            if (user == null)
            {
                ModelState.AddModelError("", "Пользователь с таким email не найден.");
                return View(null);
            }

            return View(user);
        }
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReturnBook(int recordId, string email)
        {
            var record = await _context.BorrowRecords
                .Include(r => r.Book)
                .FirstOrDefaultAsync(r => r.Id == recordId && r.ReturnDate == null);

            if (record != null)
            {
                record.ReturnDate = DateTime.UtcNow;
                record.Book.IsAvailable = true; 
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Account), new { email = email });
        }
    }
}