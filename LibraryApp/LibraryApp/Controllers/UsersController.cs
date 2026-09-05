using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LibraryApp.Data;
using LibraryApp.Models;

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
        
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(User user)
        {
            if (ModelState.IsValid)
            {
                bool emailExists = await _context.Users.AnyAsync(u => u.Email == user.Email);
                if (emailExists)
                {
                    ModelState.AddModelError("Email", "Пользователь с таким email уже зарегистрирован.");
                    return View(user);
                }

                _context.Users.Add(user);
                await _context.SaveChangesAsync();
                
                return RedirectToAction(nameof(Account), new { email = user.Email });
            }

            return View(user);
        }
    }
}