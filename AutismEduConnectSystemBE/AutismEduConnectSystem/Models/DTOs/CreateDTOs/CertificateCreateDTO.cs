using System.ComponentModel.DataAnnotations;

namespace AutismEduConnectSystem.Models.DTOs.CreateDTOs
{
    public class CertificateCreateDTO
    {
        [Required(ErrorMessage = SD.CERTIFICATE_NAME_REQUIRED)]
        public string CertificateName { get; set; }
        public string? IssuingInstitution { get; set; }
        public string? IdentityCardNumber { get; set; }
        public DateTime? IssuingDate { get; set; }
        public DateTime? ExpirationDate { get; set; }
        public List<IFormFile> Medias { get; set; }
    }
}
