namespace AutismEduConnectSystem.DTOs
{
    public class CertificateMediaDTO
    {
        public int Id { get; set; }
        public string? UrlPath { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime UpdatedDate { get; set; }
    }
}
