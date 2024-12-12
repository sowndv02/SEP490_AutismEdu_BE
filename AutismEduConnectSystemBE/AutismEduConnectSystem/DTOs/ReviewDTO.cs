using System.ComponentModel.DataAnnotations.Schema;

namespace AutismEduConnectSystem.DTOs
{
    public class ReviewDTO
    {
        public int Id { get; set; }
        public decimal RateScore { get; set; }
        public string Description { get; set; }
        public bool IsHide { get; set; }
        public ApplicationUserDTO Parent { get; set; }
        public string TutorId { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime? UpdatedDate { get; set; }
    }
}
