namespace AutismEduConnectSystem.DTOs
{
    public class InitialAssessmentResultDTO
    {
        public int Id { get; set; }
        public int QuestionId { get; set; }
        public string? Question { get; set; }
        public int OptionId { get; set; }
        public string? OptionText { get; set; }
        public double Point { get; set; }
        public bool isInitialAssessment { get; set; }
    }
}
