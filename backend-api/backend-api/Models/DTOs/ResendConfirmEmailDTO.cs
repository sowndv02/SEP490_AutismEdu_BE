using System.ComponentModel.DataAnnotations;

namespace backend_api.Models.DTOs
{
    public class ResendConfirmEmailDTO
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }
    }
}
