using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using static AutismEduConnectSystem.SD;

namespace AutismEduConnectSystem.DTOs
{
    public class ExerciseTypeDTO
    {
        public int Id { get; set; }
        public string ExerciseTypeName { get; set; }
        public List<ExerciseDTO> Exercises { get; set; }
        public bool IsHide { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime UpdatedDate { get; set; }
    }
}
