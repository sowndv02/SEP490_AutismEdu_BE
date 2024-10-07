namespace backend_api.Models.DTOs.CreateDTOs
{
    public class ProgressReportCreateDTO
    {
        public int ChildId { get; set; }
        public DateTime From { get; set; }
        public DateTime To { get; set; }
        public string? Achieved { get; set; }
        public string? Failed { get; set; }
        public string? NoteFromTutor { get; set; }
        public List<AssessmentResultCreateDTO> AssessmentResults { get; set; }
    }
}
