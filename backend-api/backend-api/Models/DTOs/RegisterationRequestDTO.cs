using System.ComponentModel.DataAnnotations;
using System.ComponentModel;

namespace backend_api.Models.DTOs
{
    public class RegisterationRequestDTO
    {
        [Required]
        [EmailAddress]
        public string UserName { get; set; }
        [Required]
        [PasswordPropertyText]
        public string Password { get; set; }
        [Required]
        public string Role { get; set; } = "client";
    }
}
