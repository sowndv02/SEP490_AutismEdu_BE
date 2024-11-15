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
        public bool IsActive { get; set; } = true;
        public bool IsHidden { get; set; } = false;
        public int? TestId { get; set; }
        [ForeignKey(nameof(TestId))]
        public Test Test { get; set; }
        public int VersionNumber { get; set; } = 1;
        public int? OriginalId { get; set; }
        [ForeignKey(nameof(OriginalId))]
        public AssessmentQuestion? Original { get; set; }
        public List<AssessmentOption> AssessmentOptions { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime? UpdatedDate { get; set; }
    }
}
