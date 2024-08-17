using Microsoft.AspNetCore.Identity;

namespace backend_api.Models
{
    public class UserRole
    {
        public string UserId { get; set; }
        public List<IdentityRole> Roles { get; set; }
    }
}
