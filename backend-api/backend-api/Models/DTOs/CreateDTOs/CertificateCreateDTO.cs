namespace backend_api.Models.DTOs.CreateDTOs
{
    public class CertificateCreateDTO
    {
        public string CertificateName { get; set; }
        public string? IssuingInstitution { get; set; }
        public DateTime? IssuingDate { get; set; }
        public DateTime? ExpirationDate { get; set; }
        public List<IFormFile> Medias { get; set; }
    }
}
