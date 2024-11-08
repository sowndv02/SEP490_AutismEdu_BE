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
        public string ParentId { get; set; }
        public double TotalPoint { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        [ForeignKey(nameof(TestId))]
        public Test Test { get; set; }
        [ForeignKey(nameof(ParentId))]
        public ApplicationUser Parent { get; set; }
        public List<TestResultDetail> Results { get; set; }
    }
}
