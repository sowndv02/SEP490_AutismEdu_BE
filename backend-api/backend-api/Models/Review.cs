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
        public string ParentId { get; set; }
        public string TutorId { get; set; }

        [ForeignKey(nameof(ParentId))]
        public ApplicationUser Parent { get; set; }

        [ForeignKey(nameof(TutorId))]
        public Tutor Tutor { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime? UpdatedDate { get; set; }
    }
}
