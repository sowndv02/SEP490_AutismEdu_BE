namespace backend_api.Models.DTOs
{
    public class AssessmentScoreRangeDTO
    {
        public int Id { get; set; }
        public string Description { get; set; }
        public float MinScore { get; set; }
        public float MaxScore { get; set; }
        public DateTime CreateDate { get; set; } = DateTime.Now;
        public DateTime? UpdateDate { get; set; }
    }
}
