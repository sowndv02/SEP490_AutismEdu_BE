using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend_api.Models
{
    public class Review
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public decimal RateScore { get; set; }
        public string Description { get; set; }
        public string ReviewerId { get; set; }
        public string RevieweeId { get; set; }

        [ForeignKey(nameof(ReviewerId))]
        public ApplicationUser Reviewer { get; set; }

        [ForeignKey(nameof(RevieweeId))]
        public Tutor Reviewee { get; set; }
    }
}
