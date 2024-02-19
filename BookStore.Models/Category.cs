using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace BookStore.Models
{
    public class Category
    {
        [Key]
        public int Id { get; set; }
        [Required]
        [DisplayName("Category Name")]
        [MaxLength(30, ErrorMessage = "Category Name Must be less than 30 Chareters.")]
        public string? Name { get; set; }
        [DisplayName("Display Order")]
        [Range(1, 100, ErrorMessage = "Display Order Must be Between 1 To 100.")]
        public int DisplayOrder { get; set; }
    }
}
