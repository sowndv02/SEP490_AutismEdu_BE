using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using static backend_api.SD;

namespace backend_api.Models
{
    public class Report
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public string? ReporterId { get; set; }
        public string Title { get; set; }
        public string? Email { get; set; }
        public ReportType ReportType { get; set; }
        public string Description { get; set; }
        public Status Status { get; set; } = SD.Status.PENDING;
        public string? Comments { get; set; }
        public string? HandlerId { get; set; }
        [ForeignKey(nameof(HandlerId))]
        public ApplicationUser Handler { get; set; }
        public string? TutorId { get; set; }
        [ForeignKey(nameof(TutorId))]
        public Tutor Tutor { get; set; }
        public int? ReviewId { get; set; }
        [ForeignKey(nameof(ReviewId))]
        public Review Review { get; set; }
        [ForeignKey(nameof(ReporterId))]
        public ApplicationUser Reporter { get; set; }
        public List<ReportMedia>? ReportMedias { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime? UpdatedDate { get; set; }
    }
}
