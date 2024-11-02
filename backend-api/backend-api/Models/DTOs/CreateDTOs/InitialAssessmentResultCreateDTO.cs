using System.ComponentModel.DataAnnotations;

namespace backend_api.Models.DTOs.CreateDTOs
{
    public class InitialAssessmentResultCreateDTO
    {
        [Required(ErrorMessage = SD.ID)]
        public int OptionId { get; set; }
        [Required(ErrorMessage = SD.ID)]
        public int QuestionId { get; set; }
    }
}
