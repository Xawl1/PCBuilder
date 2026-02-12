using System.ComponentModel.DataAnnotations;

namespace PCBuilder.Models
{
    public class RegisterViewModel
    {
        [Required, MinLength(5)]
        public string Username { get; set; } = "";

        [Required, MinLength(6)]
        public string Password { get; set; } = "";

        [Required, Compare(nameof(Password))]
        public string ConfirmPassword { get; set; } = "";
    }
}
