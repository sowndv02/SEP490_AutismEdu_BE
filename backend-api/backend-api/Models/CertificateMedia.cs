using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend_api.Models
{
    public class CertificateMedia
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public int CertificateId { get; set; }
        public string? UrlPath { get; set; }
        [ForeignKey(nameof(CertificateId))]
        public Certificate Certificate { get; set; }
    }
}
