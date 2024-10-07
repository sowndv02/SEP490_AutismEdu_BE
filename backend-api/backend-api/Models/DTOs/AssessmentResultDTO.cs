namespace backend_api.Models.DTOs
{
    public class AssessmentResultDTO
    {
        public int Id { get; set; }
        public string? Question { get; set; }
        public string? SelectedOptionText { get; set; }
        public int Point { get; set; }
    }
}
