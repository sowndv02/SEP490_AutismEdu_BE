using System.ComponentModel.DataAnnotations;

namespace AutismEduConnectSystem.Models.DTOs.CreateDTOs
{
    public class TestResultDetailCreateDTO
    {
        [Required(ErrorMessage = SD.ID_REQUIRED)]
        public int QuestionId { get; set; }
        [Required(ErrorMessage = SD.ID_REQUIRED)]
        public int OptionId { get; set; }
    }
}
