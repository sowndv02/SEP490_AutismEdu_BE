using System.ComponentModel.DataAnnotations;

namespace backend_api.Models.DTOs.CreateDTOs
{
    public class AssessmentScoreRangeCreateDTO
    {
        [Required]
        public string Description { get; set; }
        [Required]
        public float MinScore { get; set; }
        [Required]
        public float MaxScore { get; set; }
    }
}
