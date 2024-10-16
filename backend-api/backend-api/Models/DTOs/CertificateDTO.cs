﻿using static backend_api.SD;

namespace backend_api.Models.DTOs
{
    public class CertificateDTO
    {
        public int Id { get; set; }
        public string CertificateName { get; set; }
        public ApplicationUserDTO? ApprovedBy { get; set; }
        public string? IdentityCardNumber { get; set; }
        public string? IssuingInstitution { get; set; }
        public DateTime? IssuingDate { get; set; }
        public DateTime? ExpirationDate { get; set; }
        public bool IsDeleted { get; set; }
        public string? RejectionReason { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime? UpdatedDate { get; set; }
        public TutorInfoDTO? Submiter { get; set; }
        public string? Feedback { get; set; }
        public Status RequestStatus { get; set; }
        public List<CertificateMediaDTO> CertificateMedias { get; set; }
    }
}
