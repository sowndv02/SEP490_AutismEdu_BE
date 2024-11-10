using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend_api.Models
{
    public class Conversation
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public string ParentId { get; set; }
        public string TutorId { get; set; }
        [ForeignKey(nameof(TutorId))]
        public Tutor Tutor { get; set; }
        [ForeignKey(nameof(ParentId))]
        public ApplicationUser Parent { get; set; }
        public bool IsDeleted { get; set; } = false;
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime? UpdatedDate { get; set; }
    }
}
