﻿using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using static AutismEduConnectSystem.SD;

namespace AutismEduConnectSystem.Models
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
        public bool IsDeleted { get; set; } = false;
        public string? RejectionReason { get; set; }
        [ForeignKey(nameof(ApprovedId))]
        public ApplicationUser? ApprovedBy { get; set; }
        public Status RequestStatus { get; set; } = Status.PENDING;
        public string? SubmitterId { get; set; }
        [ForeignKey(nameof(SubmitterId))]
        public Tutor? Submitter { get; set; }
        public List<CertificateMedia> CertificateMedias { get; set; }
    }
}
