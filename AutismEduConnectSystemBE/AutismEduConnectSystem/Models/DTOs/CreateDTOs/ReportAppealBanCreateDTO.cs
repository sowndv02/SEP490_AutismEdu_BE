using static AutismEduConnectSystem.SD;
using System.ComponentModel.DataAnnotations.Schema;

namespace AutismEduConnectSystem.Models.DTOs.CreateDTOs
{
    public class ReportAppealBanCreateDTO
    {
        public string Title { get; set; }
        public string Email { get; set; }
        public string Description { get; set; }
    }
}
