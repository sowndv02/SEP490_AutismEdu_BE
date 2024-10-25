using System.ComponentModel.DataAnnotations.Schema;

namespace backend_api.Models.DTOs
{
    public class SyllabusExerciseDTO
    {
        public ExerciseTypeDTO ExerciseType { get; set; }

        public ExerciseDTO Exercise { get; set; }

        public DateTime CreatedDate { get; set; }
    }
}
