using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AutismEduConnectSystem.Models
{
    public class Test
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public string TestName { get; set; }
        public string TestDescription { get; set; }
        public string CreatedBy { get; set; }
        public bool IsHidden { get; set; } = false;
        public bool IsActive { get; set; } = true;
        [ForeignKey(nameof(CreatedBy))]
        public ApplicationUser User { get; set; }
        public int VersionNumber { get; set; } = 1;
        public int? OriginalId { get; set; }
        [ForeignKey(nameof(OriginalId))]
        public Test? Original { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime? UpdatedDate {  get; set; }
    }
}
