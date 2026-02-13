using System.ComponentModel.DataAnnotations;

namespace PCBuilder.Models
{
    public class User
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Username { get; set; } = "";

        [Required]
        public string PasswordHash { get; set; } = "";

        [Required]
        public string Role { get; set; } = "User";

        public ICollection<UserBuild> UserBuilds { get; set; } = new List<UserBuild>();
    }
}