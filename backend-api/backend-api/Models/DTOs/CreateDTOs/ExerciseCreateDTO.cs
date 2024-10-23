namespace backend_api.Models.DTOs.CreateDTOs
{
    public class ExerciseCreateDTO
    {
        public string ExerciseName { get; set; }
        public string ExerciseContent { get; set; }
        public int ExerciseTypeId { get; set; }
        public string TutorId { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime? UpdatedDate { get; set; }
    }
}
