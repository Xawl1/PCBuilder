using System.ComponentModel.DataAnnotations;

namespace PCBuilder.Models
{
    public class UserBuild
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        public int ProductId { get; set; }

        // Navigation properties
        public User? User { get; set; }
        public Product? Product { get; set; }
    }
}