using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AutismEduConnectSystem.Models
{
    public class AssessmentScoreRange
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public string Description { get; set; }
        public float MinScore { get; set; }
        public float MaxScore { get; set; }
        public string CreateBy { get; set; }
        public DateTime CreateDate { get; set; } = DateTime.Now;
        public DateTime? UpdateDate { get; set; }
        [ForeignKey(nameof(CreateBy))]
        public ApplicationUser User { get; set; }
    }
}
