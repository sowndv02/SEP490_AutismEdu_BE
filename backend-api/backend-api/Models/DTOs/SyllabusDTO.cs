namespace backend_api.Models.DTOs
{
    public class SyllabusDTO
    {
        public int Id { get; set; }
        public int AgeFrom { get; set; }
        public int AgeEnd { get; set; }
        public List<ExerciseTypeDTO> ExerciseType { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime UpdatedDate { get; set; }
    }
}
