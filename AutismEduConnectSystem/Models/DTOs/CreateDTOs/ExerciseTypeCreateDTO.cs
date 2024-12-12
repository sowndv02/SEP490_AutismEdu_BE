using System.ComponentModel.DataAnnotations;

namespace AutismEduConnectSystem.Models.DTOs.CreateDTOs
{
    public class ExerciseTypeCreateDTO
    {
        [Required(ErrorMessage = SD.EXERCISETYPENAME_REQUIRED)]
        public string ExerciseTypeName { get; set; }
        public bool IsHide { get; set; }
    }
}
