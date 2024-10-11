using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using static backend_api.SD;

namespace backend_api.Models
{
    public class WorkExperience
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public int? TutorRegistrationRequestId { get; set; }
        public TutorRegistrationRequest? TutorRegistrationRequest { get; set; }
        public string? SubmiterId { get; set; }
        [ForeignKey(nameof(SubmiterId))]
        public ApplicationUser? Submiter { get; set; }
        public string CompanyName { get; set; }
        public string Position { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public Status RequestStatus { get; set; } = Status.PENDING;
        public string? ApprovedId { get; set; }
        public bool IsActive { get; set; } = false;
        public int VersionNumber { get; set; } = 1;
        public int? OriginalId { get; set; }
        [ForeignKey(nameof(OriginalId))]
        public WorkExperience? OriginalWorkExperience { get; set; }
        public string? RejectionReason { get; set; }
        [ForeignKey(nameof(ApprovedId))]
        public ApplicationUser? ApprovedBy { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime UpdatedDate { get; set; }
    }
}
