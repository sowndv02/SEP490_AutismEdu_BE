using System.ComponentModel.DataAnnotations;

namespace backend_api.Models.DTOs.CreateDTOs
{
    public class UserCreateDTO
    {
        public string? Id { get; set; }
        public string Email { get; set; }
        public string FullName { get; set; }
        public string PhoneNumber { get; set; }
        public string? Address { get; set; }
<<<<<<< HEAD
        [DataType(DataType.Password)]
=======
>>>>>>> 5598c1832bd23a189aad54969380111a502c987f
        public string Password { get; set; }
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Password and ConfirmPassword do not match.")]
        public string ConfirmPassword { get; set; }
        public List<string>? RoleIds { get; set; }
        public bool IsLockedOut { get; set; }
    }
}
