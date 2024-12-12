using static AutismEduConnectSystem.SD;
using System.ComponentModel.DataAnnotations.Schema;

namespace AutismEduConnectSystem.Models.DTOs
{
    public class ReportAppealBanDTO
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Email { get; set; }
        public ReportType ReportType { get; set; }
        public string Description { get; set; }
        public Status Status { get; set; }
        public string? Comments { get; set; }
        public ApplicationUserDTO Handler { get; set; }
        public ApplicationUserDTO? Reporter { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? UpdatedDate { get; set; }
    }
}
