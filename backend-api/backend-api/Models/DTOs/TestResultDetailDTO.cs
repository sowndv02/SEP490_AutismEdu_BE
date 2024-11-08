namespace backend_api.Models.DTOs
{
    public class TestResultDetailDTO
    {
        public int Id { get; set; }
        public int TestResultId { get; set; }
        public int QuestionId { get; set; }
        public int OptionId { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.Now;
    }
}
