using System.ComponentModel.DataAnnotations.Schema;

namespace backend_api.Models.DTOs
{
    public class ReviewDTO
    {
        public int Id { get; set; }
        public decimal RateScore { get; set; }
        public string Description { get; set; }
        public string ParentId { get; set; }
        public string TutorId { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime? UpdatedDate { get; set; }
    }
}
