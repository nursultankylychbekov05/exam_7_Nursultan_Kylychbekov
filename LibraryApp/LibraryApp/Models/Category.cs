using System.ComponentModel.DataAnnotations;

namespace LibraryApp.Models
{
    public class Category
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Введите название категории")]
        public string Name { get; set; }
        
        public ICollection<Book> Books { get; set; } = new List<Book>();
    }
}