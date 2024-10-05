using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using static backend_api.SD;

namespace backend_api.Models
{
    public class Certificate
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public int? TutorRegistrationRequestId { get; set; }
        public TutorRegistrationRequest? TutorRegistrationRequest { get; set; }
        public string CertificateName { get; set; }
        public string? IdentityCardNumber { get; set; }
        public string? IssuingInstitution { get; set; }
        public DateTime? IssuingDate { get; set; }
        public DateTime? ExpirationDate { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime? UpdatedDate { get; set; }
        public string? ApprovedId { get; set; }
        public string? RejectionReason { get; set; }
        [ForeignKey(nameof(ApprovedId))]
        public ApplicationUser? ApprovedBy { get; set; }
        public Status RequestStatus { get; set; } = Status.PENDING;
        public string? SubmiterId { get; set; }
        [ForeignKey(nameof(SubmiterId))]
        public ApplicationUser? Submiter { get; set; }
        public List<CertificateMedia> CertificateMedias { get; set; }
    }
}
