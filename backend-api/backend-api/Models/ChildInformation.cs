using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend_api.Models
{
    public class ChildInformation
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public string ParentId { get; set; }
        public string? Name { get; set; }
        public bool isMale { get; set; }
        public DateTime? BirthDate { get; set; }
        public string? InitialCondition { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime? UpdatedDate { get; set; }
        [ForeignKey(nameof(ParentId))]
        public ApplicationUser Parent { get; set; }
    }
}
