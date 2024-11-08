using System.ComponentModel.DataAnnotations;

namespace backend_api.Models.DTOs.CreateDTOs
{
    public class TestResultDetailCreateDTO
    {
        [Required(ErrorMessage = SD.ID_REQUIRED)]
        public int QuestionId { get; set; }
        [Required(ErrorMessage = SD.ID_REQUIRED)]
        public int OptionId { get; set; }
    }
}
