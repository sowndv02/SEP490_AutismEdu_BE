using System.ComponentModel.DataAnnotations;

namespace backend_api.Models.DTOs
{
    public class UpdatePasswordRequestDTO
    {
        [Required]
        public string Id { get; set; }
        [DataType(DataType.Password)]
        [Required]  
        public string OldPassword { get; set; }
        [DataType(DataType.Password)]
        public string NewPassword { get; set; }
        [DataType(DataType.Password)]
        [Compare("NewPassword", ErrorMessage = "Password and ConfirmPassword do not match.")]
        public string ConfirmNewPassword { get; set; }
    }
}
