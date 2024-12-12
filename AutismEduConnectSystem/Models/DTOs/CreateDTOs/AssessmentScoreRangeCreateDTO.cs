using System.ComponentModel.DataAnnotations;

namespace AutismEduConnectSystem.Models.DTOs.CreateDTOs
{
    public class AssessmentScoreRangeCreateDTO
    {
        [Required(ErrorMessage = SD.QUESTION_REQUIRED)]
        public string Description { get; set; }
        [Required(ErrorMessage = SD.POINT_REQUIRED)]
        public float MinScore { get; set; }
        [Required(ErrorMessage = SD.POINT_REQUIRED)]
        public float MaxScore { get; set; }
    }
}
