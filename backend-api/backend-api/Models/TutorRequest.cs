using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend_api.Models
{
    public class TutorRequest
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public int ChildId { get; set; }
        [ForeignKey(nameof(ChildId))]
        public ChildInformation ChildInformation { get; set; }
        public string UserId { get; set; }
        [ForeignKey(nameof(UserId))]
        public ApplicationUser Parent { get; set; }
        public string? Description { get; set; }
        public string TutorId { get; set; }
        [ForeignKey(nameof(TutorId))]
        public Tutor Tutor { get; set; }

    }
}
