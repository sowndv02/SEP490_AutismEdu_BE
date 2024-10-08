using System.ComponentModel.DataAnnotations;

namespace backend_api.Models.DTOs.CreateDTOs
{
    public class TutorProfileUpdateRequestCreateDTO
    {
        public decimal? Price { get; set; }
        [Required]
        public string Address { get; set; }
        public string? AboutMe { get; set; }
    }
}
