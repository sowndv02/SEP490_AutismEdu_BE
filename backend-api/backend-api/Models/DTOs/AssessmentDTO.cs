namespace backend_api.Models.DTOs
{
    public class AssessmentDTO
    {
        public string? Condition { get; set; }
        public List<InitialAssessmentResultDTO> AssessmentResults { get; set; }
    }
}
