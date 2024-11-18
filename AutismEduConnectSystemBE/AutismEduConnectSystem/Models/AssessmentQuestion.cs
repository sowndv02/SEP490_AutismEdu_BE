using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AutismEduConnectSystem.Models
{
    public class AssessmentQuestion
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public string? Question { get; set; }
        public string SubmitterId { get; set; }
        [ForeignKey(nameof(SubmitterId))]
        public ApplicationUser Submitter { get; set; }
        public bool IsHidden { get; set; }
        public int? TestId { get; set; }
        [ForeignKey(nameof(TestId))]
        public List<AssessmentOption> AssessmentOptions { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime? UpdatedDate { get; set; }
    }
}
