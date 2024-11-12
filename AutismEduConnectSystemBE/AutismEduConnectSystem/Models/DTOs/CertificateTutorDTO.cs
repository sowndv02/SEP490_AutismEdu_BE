using static AutismEduConnectSystem.SD;

namespace AutismEduConnectSystem.Models.DTOs
{
    public class CertificateTutorDTO
    {
        public int Id { get; set; }
        public string CertificateName { get; set; }
        public string? IdentityCardNumber { get; set; }
        public string? IssuingInstitution { get; set; }
        public DateTime? IssuingDate { get; set; }
        public DateTime? ExpirationDate { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime? UpdatedDate { get; set; }
        public string? Feedback { get; set; }
        public Status RequestStatus { get; set; }
        public List<CertificateMediaDTO> CertificateMedias { get; set; }
    }
}
