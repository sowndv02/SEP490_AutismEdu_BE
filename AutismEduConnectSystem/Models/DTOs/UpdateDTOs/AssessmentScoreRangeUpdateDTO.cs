namespace AutismEduConnectSystem.Models.DTOs.UpdateDTOs
{
    public class AssessmentScoreRangeUpdateDTO
    {
        public int Id { get; set; }
        public string Description { get; set; }
        public float MinScore { get; set; }
        public float MaxScore { get; set; }
    }
}
