namespace backend_api.Models.DTOs.UpdateDTOs
{
    public class ReviewUpdateDTO
    {
        public decimal RateScore { get; set; }
        public string Description { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? UpdatedDate { get; set; } = DateTime.Now;
    }
}
