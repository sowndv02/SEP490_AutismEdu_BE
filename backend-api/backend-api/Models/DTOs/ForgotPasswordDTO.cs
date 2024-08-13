using System.ComponentModel.DataAnnotations;

namespace backend_api.Models.DTOs
{
    public class ForgotPasswordDTO
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }
    }
}
