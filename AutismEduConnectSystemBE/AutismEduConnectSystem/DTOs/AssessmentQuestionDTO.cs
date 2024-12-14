namespace AutismEduConnectSystem.DTOs
{
    public class AssessmentQuestionDTO
    {
        public int Id { get; set; }
        public string? Question { get; set; }
        public DateTime CreatedDate { get; set; }
        public List<AssessmentOptionDTO> AssessmentOptions { get; set; }
    }
}
