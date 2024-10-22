using System.ComponentModel.DataAnnotations;

namespace backend_api.Models.DTOs.CreateDTOs
{
    public class ChildInformationCreateDTO
    {
        [Required]
        public string? Name { get; set; }
        [Required]
        public bool? isMale { get; set; }
        [Required]
        public DateTime? BirthDate { get; set; }
        public IFormFile? Media { get; set; }
    }
}
