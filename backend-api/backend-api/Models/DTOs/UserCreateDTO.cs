using System.ComponentModel.DataAnnotations;

namespace backend_api.Models.DTOs
{
    public class UserCreateDTO
    {
        public string? Id { get; set; }
        public string Email { get; set; }
        public string FullName { get; set; }
        public string PhoneNumber { get; set; }
        public string? Address { get; set; }
        public string Password { get; set; }
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Password and ConfirmPassword do not match.")]
        public string ConfirmPassword { get; set; }
        public List<string>? RoleIds { get; set; }
        public bool IsLockedOut { get; set; }
    }
}
