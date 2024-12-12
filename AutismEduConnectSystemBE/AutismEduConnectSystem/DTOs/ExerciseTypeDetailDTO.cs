namespace AutismEduConnectSystem.DTOs
{
    public class ExerciseTypeDetailDTO
    {
        public int Id { get; set; }
        public string ExerciseTypeName { get; set; }
        public int TotalExercises { get; set; }
        public bool IsHide { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime UpdatedDate { get; set; }
    }
}
