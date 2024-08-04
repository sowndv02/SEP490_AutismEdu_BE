using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace backend_api.Models
{
    public class ApplicationUser :IdentityUser
    {
        [Required]
        public string FullName { get; set; }
        public DateTime DateCreated { get; set; }

        [NotMapped]
        public string RoleId { get; set; }
        [NotMapped]
        public string Role { get; set; }
        [NotMapped]
        public string UserClaim { get; set; }
        [NotMapped]
        public bool IsLockedOut { get; set; }
    }
}
