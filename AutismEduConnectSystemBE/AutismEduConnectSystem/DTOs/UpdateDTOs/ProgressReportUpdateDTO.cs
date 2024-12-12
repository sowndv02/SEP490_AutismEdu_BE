namespace AutismEduConnectSystem.DTOs.UpdateDTOs
{
    public class ProgressReportUpdateDTO
    {
        public int Id { get; set; }
        public string? Achieved { get; set; }
        public string? Failed { get; set; }
        public string? NoteFromTutor { get; set; }
        public List<AssessmentResultUpdateDTO> AssessmentResults { get; set; }
    }
}
