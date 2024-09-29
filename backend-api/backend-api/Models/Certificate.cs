using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend_api.Models
{
    public class Certificate
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public string SubmiterId { get; set; }
        public string CertificateName { get; set; }
        public string? IssuingInstitution { get; set; }
        public DateTime? IssuingDate { get; set; }
        public DateTime? ExpirationDate { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime? UpdatedDate { get; set; }
        public bool? IsApprove { get; set; }
        [ForeignKey(nameof(SubmiterId))]
        public ApplicationUser Submiter { get; set; }
    }
}
