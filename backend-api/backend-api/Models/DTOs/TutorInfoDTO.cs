using System.ComponentModel.DataAnnotations;

namespace backend_api.Models.DTOs
{
    public class TutorInfoDTO
    {
        public string UserId { get; set; }
        public string FormalName { get; set; }
        public DateTime DateOfBirth { get; set; }
        public int StartAge { get; set; }
        public int EndAge { get; set; }
        public bool IsApprove { get; set; }
        public string? AboutMe { get; set; }
        public decimal? PriceFrom { get; set; }
        public decimal? PriceTo { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? UpdatedDate { get; set; }
    }
}
