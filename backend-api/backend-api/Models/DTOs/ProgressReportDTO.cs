namespace backend_api.Models.DTOs
{
    public class ProgressReportDTO
    {
        public int Id { get; set; }
        public DateTime From { get; set; }
        public DateTime To { get; set; }
        public string? Achieved { get; set; }
        public string? Failed { get; set; }
        public string? NoteFromTutor { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        List<AssessmentResultDTO> AssessmentResults { get; set; }
    }
}
