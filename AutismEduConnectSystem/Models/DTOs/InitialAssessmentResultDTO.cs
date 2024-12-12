namespace AutismEduConnectSystem.Models.DTOs
{
    public class InitialAssessmentResultDTO
    {
        public int Id { get; set; }
        public string? Question { get; set; }
        public string? OptionText { get; set; }
        public double Point { get; set; }
        public bool isInitialAssessment { get; set; }
    }
}
