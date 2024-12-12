using System.ComponentModel.DataAnnotations;

namespace AutismEduConnectSystem.Models.DTOs
{
    public class ForgotPasswordDTO
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }
    }
}
