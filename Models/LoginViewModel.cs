using System.ComponentModel.DataAnnotations;

namespace PCBuilder.Models
{
    public class LoginViewModel
    {
        [Required]
        public string Username { get; set; } = "";

        [Required]
        public string Password { get; set; } = "";
    }
}
