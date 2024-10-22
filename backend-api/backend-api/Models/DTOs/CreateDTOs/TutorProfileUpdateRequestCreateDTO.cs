using System.ComponentModel.DataAnnotations;

namespace backend_api.Models.DTOs.CreateDTOs
{
    public class TutorProfileUpdateRequestCreateDTO
    {
        [Required]
        public decimal PriceFrom { get; set; }
        public decimal PriceEnd { get; set; }
        public float SessionHours { get; set; }
        [Required]
        public string Address { get; set; }
        [Required]
        public string AboutMe { get; set; }
        [Required]
        public string PhoneNumber { get; set; }
        [Required]
        public int StartAge { get; set; }
        [Required]
        public int EndAge { get; set; }
    }
}
