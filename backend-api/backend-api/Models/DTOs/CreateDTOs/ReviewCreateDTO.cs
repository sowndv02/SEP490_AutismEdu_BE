namespace backend_api.Models.DTOs.CreateDTOs
{
    public class ReviewCreateDTO
    {
        public decimal RateScore { get; set; }
        public string Description { get; set; }
        public string TutorId { get; set; }
    }
}
