using System.ComponentModel.DataAnnotations;

namespace PCBuilder.Models
{
    public class Product
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int CategoryId { get; set; }

        [Required]
        [StringLength(50)]
        public string Brand { get; set; } = "";

        [Required]
        [StringLength(100)]
        public string ModelName { get; set; } = "";

        [Required]
        [Range(0, 999999)]
        public decimal Price { get; set; }

        [Required]
        [Range(1, 3)]
        public int Tier { get; set; }

        // Navigation properties
        public Category? Category { get; set; }
        public ICollection<UserBuild> UserBuilds { get; set; } = new List<UserBuild>();
    }
}