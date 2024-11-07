using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend_api.Models
{
    public class TestResult
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public int TestId { get; set; }
        public int QuestionId { get; set; }
        public int OptionId { get; set; }
        public int ParentId { get; set; }
        public int VersionNumber { get; set; } = 1;
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime? UpdatedDate { get; set; }
        [ForeignKey(nameof(TestId))]
        public Test Test { get; set; }
        [ForeignKey(nameof(QuestionId))]
        public AssessmentQuestion Question { get; set; }
        [ForeignKey(nameof(OptionId))]
        public AssessmentOption Option { get; set; }
        [ForeignKey(nameof(ParentId))]
        public ApplicationUser Parent { get; set; }
    }
}
