namespace backend_api.Models.DTOs.CreateDTOs
{
    public class TestResultCreateDTO
    {
        public int TestId { get; set; }
        public List<AssessmentResultCreateDTO> TestResults { get; set; }
    }
}
