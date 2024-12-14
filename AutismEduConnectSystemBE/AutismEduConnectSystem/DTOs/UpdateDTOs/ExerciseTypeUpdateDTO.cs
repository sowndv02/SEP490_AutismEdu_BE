using System.ComponentModel.DataAnnotations;

namespace AutismEduConnectSystem.DTOs.UpdateDTOs
{
    public class ExerciseTypeUpdateDTO
    {
        public int Id { get; set; }
        [Required(ErrorMessage = SD.EXERCISETYPENAME_REQUIRED)]
        public string ExerciseTypeName { get; set; }
    }
}
