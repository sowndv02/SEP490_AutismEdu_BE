namespace backend_api.Models.DTOs
{
    public class TestResultDTO
    {
        public int Id { get; set; }
        public int TestId { get; set; }
        public string TestName { get; set; }
        public string TestDescription { get; set; }
        public double TotalPoint { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public List<AssessmentQuestionDTO> TestQuestions { get; set; }
        public List<TestResultDetailDTO> Results { get; set; }
    }
}
