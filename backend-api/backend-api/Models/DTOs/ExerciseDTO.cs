using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend_api.Models.DTOs
{
    public class ExerciseDTO
    {
        public int Id { get; set; }
        public string ExerciseName { get; set; }
        public string ExerciseContent { get; set; }
        public string TutorId { get; set; }
        public int ExerciseTypeId { get; set; }
        //public string ExerciseTypeName { get; set; }
    }
}
