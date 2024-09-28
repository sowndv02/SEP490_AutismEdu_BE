using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend_api.Models
{
    public class Tutor
    {
        [Key]
        public string UserId { get; set; }
        public string FormalName { get; set; }
        public decimal? PriceFrom { get; set; }
        public decimal? PriceTo { get; set; }
        public int StartAge { get; set; }
        public int EndAge { get; set; }
        public DateTime DateOfBirth { get; set; }
        public bool IsApprove { get; set; } = false;
        public bool IsDraft { get; set; } = false;
        [ForeignKey(nameof(UserId))]
        public ApplicationUser User { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime? UpdatedDate { get; set; }
    }
}
