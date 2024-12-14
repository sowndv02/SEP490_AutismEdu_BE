namespace AutismEduConnectSystem.DTOs
{
    public class ExerciseNotPagingDTO
    {
        public int Id { get; set; }
        public string ExerciseName { get; set; }
        public string Description { get; set; }
        public ExerciseTypeInfoDTO ExerciseType { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime UpdatedDate { get; set; }
    }
}
