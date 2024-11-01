using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using static backend_api.SD;

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
        public string ParentId { get; set; }
        [ForeignKey(nameof(ParentId))]
        public ApplicationUser Parent { get; set; }
        public string? Description { get; set; }
        public Status RequestStatus { get; set; } = Status.PENDING;
        public string? RejectionReason { get; set; }
        public RejectType RejectType { get; set; }
        public string TutorId { get; set; }
        [ForeignKey(nameof(TutorId))]
        public Tutor Tutor { get; set; }
        public bool HasStudentProfile { get; set; } = false;
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime? UpdatedDate { get; set; }

    }
}
