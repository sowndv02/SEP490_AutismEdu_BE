namespace backend_api.Models.DTOs.CreateDTOs
{
    public class SyllabusExerciseCreateDTO
    {
        public int ExerciseTypeId { get; set; }
        public List<int> ExerciseIds { get; set; }
    }
}
