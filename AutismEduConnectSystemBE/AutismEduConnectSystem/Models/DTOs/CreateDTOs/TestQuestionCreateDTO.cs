using System.ComponentModel.DataAnnotations;

namespace AutismEduConnectSystem.Models.DTOs.CreateDTOs
{
    public class TestQuestionCreateDTO
    {
        [Required(ErrorMessage = SD.ID_REQUIRED)]
        public int TestId { get; set; }
        [Required(ErrorMessage = SD.QUESTION_REQUIRED)]
        public string Question {  get; set; }
        [Required(ErrorMessage = SD.OPTION_TEXT_REQUIRED)]
        public List<AssessmentOptionCreateDTO> Options { get; set; }
        public int? OriginalId { get; set; }
    }
}
