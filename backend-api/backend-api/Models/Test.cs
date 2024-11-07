using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend_api.Models
{
    public class Test
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public string TestName { get; set; }
        public string TestDescription { get; set; }
        public string CreatedBy { get; set; }
        [ForeignKey(nameof(CreatedBy))]
        public ApplicationUser User { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime? UpdatedDate {  get; set; }
    }
}
