namespace backend_api.Models.DTOs.UpdateDTOs
{
    public class AssignExerciseScheduleDTO
    {
        public int Id { get; set; }
        public int SyllabusId { get; set; }
        public int ExerciseId { get; set; }
        public int ExerciseTypeId { get; set; }
    }
}
