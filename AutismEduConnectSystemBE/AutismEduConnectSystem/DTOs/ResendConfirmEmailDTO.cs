using System.ComponentModel.DataAnnotations;

namespace AutismEduConnectSystem.DTOs
{
    public class ResendConfirmEmailDTO
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }
    }
}
