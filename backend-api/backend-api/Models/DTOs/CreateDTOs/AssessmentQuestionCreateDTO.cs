using System.ComponentModel.DataAnnotations;

namespace backend_api.Models.DTOs.CreateDTOs
{
    public class AssessmentQuestionCreateDTO
    {
        [Required(ErrorMessage = SD.QUESTION_REQUIRED)]
        public string Question { get; set; }
        [Required(ErrorMessage = SD.OPTION_TEXT_REQUIRED)]
        public List<AssessmentOptionCreateDTO> AssessmentOptions { get; set; }
    }
}
