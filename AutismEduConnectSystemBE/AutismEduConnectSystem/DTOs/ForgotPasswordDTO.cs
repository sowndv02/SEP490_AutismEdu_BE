using System.ComponentModel.DataAnnotations;

namespace AutismEduConnectSystem.DTOs
{
    public class ForgotPasswordDTO
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }
    }
}
