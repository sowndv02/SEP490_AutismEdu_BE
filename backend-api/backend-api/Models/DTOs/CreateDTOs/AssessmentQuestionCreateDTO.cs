using System.ComponentModel.DataAnnotations;

namespace backend_api.Models.DTOs.CreateDTOs
{
    public class AssessmentQuestionCreateDTO
    {
        [Required]
        public string Question { get; set; }
        [Required]
        public List<AssessmentOptionCreateDTO> AssessmentOptions { get; set; }
    }
}
