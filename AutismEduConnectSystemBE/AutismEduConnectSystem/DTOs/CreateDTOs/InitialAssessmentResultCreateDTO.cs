using System.ComponentModel.DataAnnotations;

namespace AutismEduConnectSystem.DTOs.CreateDTOs
{
    public class InitialAssessmentResultCreateDTO
    {
        [Required(ErrorMessage = SD.ID)]
        public int OptionId { get; set; }
        [Required(ErrorMessage = SD.ID)]
        public int QuestionId { get; set; }
    }
}
