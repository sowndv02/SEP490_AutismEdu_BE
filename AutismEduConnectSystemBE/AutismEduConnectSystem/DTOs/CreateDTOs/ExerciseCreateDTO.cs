using System.ComponentModel.DataAnnotations;

namespace AutismEduConnectSystem.DTOs.CreateDTOs
{
    public class ExerciseCreateDTO
    {
        [Required(ErrorMessage = SD.EXERCISENAME_REQUIRED)]
        public string ExerciseName { get; set; }
        [Required(ErrorMessage = SD.DESCRIPTION_REQUIRED)]
        public string Description { get; set; }
        public int ExerciseTypeId { get; set; }
        public int? OriginalId { get; set; }
    }
}
