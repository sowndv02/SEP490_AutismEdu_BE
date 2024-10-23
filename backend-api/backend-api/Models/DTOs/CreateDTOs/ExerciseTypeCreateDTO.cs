namespace backend_api.Models.DTOs.CreateDTOs
{
    public class ExerciseTypeCreateDTO
    {
        public string ExerciseTypeName { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime? UpdatedDate { get; set; }
    }
}
