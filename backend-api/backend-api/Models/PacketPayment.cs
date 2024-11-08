using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend_api.Models
{
    public class PackagePayment
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public string Title { get; set; }
        public int Duration { get; set; }
        public string? Description { get; set; }
        public double Price { get; set; }
        public int VersionNumber { get; set; } = 1;
        public int? OriginalId { get; set; }
        [ForeignKey(nameof(OriginalId))]
        public PackagePayment? Original { get; set; }
        public bool IsActive { get; set; }
        public string SubmitterId { get; set; }
        [ForeignKey(nameof(SubmitterId))]
        public ApplicationUser Submitter { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime? UpdatedDate { get; set; }
    }
}
