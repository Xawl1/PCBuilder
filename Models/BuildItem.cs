using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PCBuilder.Models
{
    public class BuildItem
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int BuildId { get; set; }

        [Required]
        public int ProductId { get; set; }

        public int Quantity { get; set; } = 1;

        // Navigation properties
        [ForeignKey("BuildId")]
        public Build? Build { get; set; }

        [ForeignKey("ProductId")]
        public Product? Product { get; set; }
    }
}