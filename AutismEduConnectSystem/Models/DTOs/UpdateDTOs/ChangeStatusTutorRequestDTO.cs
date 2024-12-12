using static AutismEduConnectSystem.SD;

namespace AutismEduConnectSystem.Models.DTOs.UpdateDTOs
{
    public class ChangeStatusTutorRequestDTO
    {
        public int Id { get; set; }
        public int StatusChange { get; set; }
        public RejectType RejectType { get; set; }
        public string? RejectionReason { get; set; }
    }
}
