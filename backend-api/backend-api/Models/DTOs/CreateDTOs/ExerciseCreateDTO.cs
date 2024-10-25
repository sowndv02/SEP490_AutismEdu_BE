namespace backend_api.Models.DTOs.CreateDTOs
{
    public class ExerciseCreateDTO
    {
        public string ExerciseName { get; set; }
        public string Description { get; set; }
        public int ExerciseTypeId { get; set; }
        public int? OriginalId { get; set; }
    }
}
