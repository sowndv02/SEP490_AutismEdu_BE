using System.ComponentModel.DataAnnotations;

namespace AutismEduConnectSystem.Models.DTOs.CreateDTOs
{
    public class InitialAssessmentResultCreateDTO
    {
        [Required(ErrorMessage = SD.ID)]
        public int OptionId { get; set; }
        [Required(ErrorMessage = SD.ID)]
        public int QuestionId { get; set; }
    }
}
