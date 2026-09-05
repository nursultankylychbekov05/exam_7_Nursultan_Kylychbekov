using System.ComponentModel.DataAnnotations;

namespace LibraryApp.Models
{
    public class User
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Введите имя")]
        public string FirstName { get; set; }

        [Required(ErrorMessage = "Введите фамилию")]
        public string LastName { get; set; }

        [Required(ErrorMessage = "Введите email")]
        [EmailAddress(ErrorMessage = "Некорректный формат email")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Введите номер телефона")]
        public string Phone { get; set; }
        
        public ICollection<BorrowRecord> BorrowRecords { get; set; } = new List<BorrowRecord>();
        
        
    }
}