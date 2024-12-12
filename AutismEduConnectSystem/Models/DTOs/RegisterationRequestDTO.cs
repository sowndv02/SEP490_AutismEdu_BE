using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace AutismEduConnectSystem.Models.DTOs
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
        public string FullName { get; set; }
        [Required]
        public string Address { get; set; }
        [Required]
        public string PhoneNumber { get; set; }
    }
}
