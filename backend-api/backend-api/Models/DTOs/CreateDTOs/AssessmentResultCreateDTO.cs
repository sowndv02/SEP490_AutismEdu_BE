using System.ComponentModel.DataAnnotations;

namespace backend_api.Models.DTOs.CreateDTOs
{
    public class AssessmentResultCreateDTO
    {
        [Required]
        public int OptionId { get; set; }
    }
}
