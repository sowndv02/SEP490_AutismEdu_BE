namespace backend_api.Models.DTOs
{
    public class CertificateDTO
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
        public bool? IsApprove { get; set; }
        public List<CertificateMedia> CertificateMedias { get; set; }
    }
}
