namespace backend_api.Models.DTOs
{
    public class ProgressReportGraphDTO
    {
        public InitialAssessmentResultDTO InitialAssessmentResultDTO { get; set; }
        public List<ProgressReportDTO> ProgressReports { get; set; }
    }
}
