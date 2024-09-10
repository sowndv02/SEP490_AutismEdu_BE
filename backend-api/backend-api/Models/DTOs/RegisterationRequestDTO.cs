using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace backend_api.Models.DTOs
{
    public class RegisterationRequestDTO
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }
        [Required]
        [PasswordPropertyText]
        public string Password { get; set; }
        [Required]
        public string Role { get; set; } = SD.USER_ROLE;
    }
}
