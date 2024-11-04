using System.ComponentModel.DataAnnotations;

namespace backend_api.Models.DTOs.UpdateDTOs
{
    public class AssessmentResultUpdateDTO
    {
        public int Id { get; set; }
        public int QuestionId { get; set; }
        public int OptionId { get; set; }
    }
}
