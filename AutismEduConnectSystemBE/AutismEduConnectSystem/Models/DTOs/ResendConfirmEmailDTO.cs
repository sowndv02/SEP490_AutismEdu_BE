using System.ComponentModel.DataAnnotations;

namespace AutismEduConnectSystem.Models.DTOs
{
    public class ResendConfirmEmailDTO
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }
    }
}
