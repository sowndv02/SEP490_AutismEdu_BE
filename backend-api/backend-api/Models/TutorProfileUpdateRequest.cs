using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using static backend_api.SD;

namespace backend_api.Models
{
    public class TutorProfileUpdateRequest
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public string TutorId { get; set; }
        public string AboutMe { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime? UpdatedDate { get; set; }
        public Status RequestStatus { get; set; } = Status.PENDING;
        public string? ApprovedId { get; set; }
        public string? RejectionReason { get; set; }
        [ForeignKey(nameof(ApprovedId))]
        public ApplicationUser? ApprovedBy { get; set; }
    }
}
