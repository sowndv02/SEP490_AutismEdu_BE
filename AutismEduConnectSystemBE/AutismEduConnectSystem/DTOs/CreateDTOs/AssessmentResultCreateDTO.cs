using System.ComponentModel.DataAnnotations;

namespace AutismEduConnectSystem.DTOs.CreateDTOs
{
    public class AssessmentResultCreateDTO
    {
        [Required(ErrorMessage = SD.QUESTION_REQUIRED)]
        public int QuestionId { get; set; }
        [Required(ErrorMessage = SD.OPTION_TEXT_REQUIRED)]
        public int OptionId { get; set; }
    }
}
