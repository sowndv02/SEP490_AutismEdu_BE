using static AutismEduConnectSystem.SD;
using System.ComponentModel.DataAnnotations.Schema;

namespace AutismEduConnectSystem.Models.DTOs
{
    public class ReportDTO
    {
        public int Id { get; set; }
        public string? ReporterId { get; set; }
        public string Title { get; set; }
        public string? Email { get; set; }
        public ReportType ReportType { get; set; }
        public string Description { get; set; }
        public Status Status { get; set; }
        public string? Comments { get; set; }
        public ApplicationUserDTO Handler { get; set; }
        public TutorDTO Tutor { get; set; }
        public ReviewDTO Review { get; set; }
        public ApplicationUserDTO Reporter { get; set; }
        public List<ReportMediaDTO>? ReportMedias { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? UpdatedDate { get; set; }
    }
}
