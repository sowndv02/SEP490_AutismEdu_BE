namespace AutismEduConnectSystem.DTOs.CreateDTOs
{
    public class SyllabusCreateDTO
    {
        public int AgeFrom { get; set; }
        public int AgeEnd { get; set; }
        public List<SyllabusExerciseCreateDTO> SyllabusExercises { get; set; }
    }
}
