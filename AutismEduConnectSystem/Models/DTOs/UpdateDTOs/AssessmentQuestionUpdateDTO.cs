namespace AutismEduConnectSystem.Models.DTOs.UpdateDTOs
{
    public class AssessmentQuestionUpdateDTO
    {
        public int Id { get; set; }
        public string? Question { get; set; }
        public List<AssessmentOptionUpdateDTO> AssessmentOptions { get; set; }
    }
}
