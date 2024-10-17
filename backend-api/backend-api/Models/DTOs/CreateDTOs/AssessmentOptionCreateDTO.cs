using System.ComponentModel.DataAnnotations;

namespace backend_api.Models.DTOs.CreateDTOs
{
    public class AssessmentOptionCreateDTO
    {
        [Required]
        public string? OptionText { get; set; }
        [Required]
        public double Point { get; set; }
    }
}
