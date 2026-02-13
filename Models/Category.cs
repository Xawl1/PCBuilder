using Microsoft.CodeAnalysis;
using System.ComponentModel.DataAnnotations;

namespace PCBuilder.Models
{
    public class Category
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string CategoryName { get; set; } = "";

        // Navigation property
        public ICollection<Product> Products { get; set; } = new List<Product>();
    }
}