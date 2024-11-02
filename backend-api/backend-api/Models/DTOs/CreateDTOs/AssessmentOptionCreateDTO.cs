using System.ComponentModel.DataAnnotations;

namespace backend_api.Models.DTOs.CreateDTOs
{
    public class AssessmentOptionCreateDTO
    {
        [Required(ErrorMessage = SD.OPTION_TEXT_REQUIRED)]
        public string OptionText { get; set; }
        [Required(ErrorMessage = SD.POINT_REQUIRED)]
        public double Point { get; set; }
    }
}
