using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PCBuilder.Models
{
    public class Build
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        public string BuildName { get; set; } = "My PC Build";

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        [ForeignKey("UserId")]
        public User? User { get; set; }

        public ICollection<BuildItem> BuildItems { get; set; } = new List<BuildItem>();
    }
}