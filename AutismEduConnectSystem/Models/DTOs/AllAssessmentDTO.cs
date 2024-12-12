namespace AutismEduConnectSystem.Models.DTOs
{
    public class AllAssessmentDTO
    {
        public List<AssessmentScoreRangeDTO> ScoreRanges { get; set; }
        public List<AssessmentQuestionDTO> Questions { get; set; }
    }
}
