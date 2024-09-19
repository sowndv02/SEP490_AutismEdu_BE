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
<<<<<<< HEAD
        public string FullName { get; set; }
=======
        public string Role { get; set; } = SD.USER_ROLE;
>>>>>>> 5598c1832bd23a189aad54969380111a502c987f
    }
}
