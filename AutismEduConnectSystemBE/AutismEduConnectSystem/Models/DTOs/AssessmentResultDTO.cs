namespace AutismEduConnectSystem.Models.DTOs
{
    public class AssessmentResultDTO
    {
        public int Id { get; set; }
        public int QuestionId { get; set; }
        public string? Question { get; set; }
        public int OptionId { get; set; }
        public string? SelectedOptionText { get; set; }
        public double Point { get; set; }
    }
}
