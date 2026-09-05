using System;
using System.ComponentModel.DataAnnotations;

namespace LibraryApp.Models
{
    public class Book
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Введите название книги")]
        public string Title { get; set; }

        [Required(ErrorMessage = "Укажите автора")]
        public string Author { get; set; }

        public string? CoverImagePath { get; set; } 

        public int? PublishYear { get; set; }

        public string? Description { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public bool IsAvailable { get; set; } = true;
    }
}