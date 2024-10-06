using System.ComponentModel.DataAnnotations;

namespace backend_api.Models.DTOs.CreateDTOs
{
    public class ClaimCreateDTO
    {
        [Required]
        public string ClaimType { get; set; }
        [Required]
        public string ClaimValue { get; set; }
    }
}
