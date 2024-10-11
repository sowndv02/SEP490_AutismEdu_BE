using System.ComponentModel.DataAnnotations;

namespace backend_api.Models.DTOs.CreateDTOs
{
    public class CertificateCreateDTO
    {
        [Required]
        public string CertificateName { get; set; }
        public string? IssuingInstitution { get; set; }
        public string? IdentityCardNumber { get; set; }
        public DateTime? IssuingDate { get; set; }
        public DateTime? ExpirationDate { get; set; }
        public List<IFormFile> Medias { get; set; }
        public int? OriginalId { get; set; }
    }
}
