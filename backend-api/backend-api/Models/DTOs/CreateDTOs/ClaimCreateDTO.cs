using System.ComponentModel.DataAnnotations;

namespace backend_api.Models.DTOs.CreateDTOs
{
    public class ClaimCreateDTO
    {
        [Required(ErrorMessage = SD.CLAIM_TYPE_REQUIRED)]
        public string ClaimType { get; set; }
        [Required(ErrorMessage = SD.CLAIM_VALUE_REQUIRED)]
        public string ClaimValue { get; set; }
    }
}
