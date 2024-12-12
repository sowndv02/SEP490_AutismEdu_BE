using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using static AutismEduConnectSystem.SD;

namespace AutismEduConnectSystem.Models
{
    public class TutorProfileUpdateRequest
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public string TutorId { get; set; }
        [ForeignKey(nameof(TutorId))]
        public Tutor Tutor { get; set; }
        public decimal PriceFrom { get; set; }
        public decimal PriceEnd { get; set; }
        public float SessionHours { get; set; }
        public string PhoneNumber { get; set; }
        public int StartAge { get; set; }
        public int EndAge { get; set; }
        public string Address { get; set; }
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
