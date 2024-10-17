namespace backend_api.Models.DTOs
{
    public class AssessmentOptionDTO
    {
        public int Id { get; set; }
        public string? OptionText { get; set; }
        public double Point { get; set; }
    }
}
