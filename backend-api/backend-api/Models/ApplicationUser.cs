using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend_api.Models
{
    public class ApplicationUser : IdentityUser
    {
        [Required]
        public string FullName { get; set; }
        public DateTime CreatedDate { get; set; }
        public string? ImageUrl { get; set; }
        [NotMapped]
        public List<string> RoleIds { get; set; }
        [NotMapped]
        public string Role { get; set; }
        [NotMapped]
        public string UserClaim { get; set; }
        [NotMapped]
        public bool IsLockedOut { get; set; }
        public string UserType { get; set; } = SD.APPLICATION_USER;

        //public List<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
        //public List<ApplicationClaim> ApplicationClaims { get; set; } = new List<ApplicationClaim>();
        public string? Address { get; set; }
        public Tutor? TutorProfile { get; set; }
    }
}
