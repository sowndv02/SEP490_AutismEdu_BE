namespace AutismEduConnectSystem.DTOs
{
    public class ExerciseDTO
    {
        public int Id { get; set; }
        public string ExerciseName { get; set; }
        public string Description { get; set; }
        public ExerciseDTO? Original { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime UpdatedDate { get; set; }
    }
}
