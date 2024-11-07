using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend_api.Models
{
    public class TestResultDetail
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public int TestResultId { get; set; }
        public int QuestionId { get; set; }
        public int OptionId { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        [ForeignKey(nameof(TestResultId))]
        public TestResult TestResult { get; set; }
        [ForeignKey(nameof(QuestionId))]
        public AssessmentQuestion Question { get; set; }
        [ForeignKey(nameof(OptionId))]
        public AssessmentOption Option { get; set; }
    }
}
