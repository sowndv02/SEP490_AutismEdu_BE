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
        public string? ImageLocalPathUrl { get; set; }
        public string? ImageLocalUrl { get; set; }
        [NotMapped]
        public string RoleId { get; set; }
        [NotMapped]
        public string Role { get; set; }
        [NotMapped]
        public string UserClaim { get; set; }
        [NotMapped]
        public bool IsLockedOut { get; set; }
        public string UserType { get; set; } = SD.APPLICATION_USER;

        //public List<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
        //public List<ApplicationClaim> ApplicationClaims { get; set; } = new List<ApplicationClaim>();

        public string? AboutMe { get; set; }
        public string? ExperienceYear { get; set; }
        public string? University { get; set; }
        public decimal? Price { get; set; }
        public string? Address { get; set; }
    }
}
