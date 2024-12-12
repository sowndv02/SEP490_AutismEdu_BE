namespace AutismEduConnectSystem.DTOs.UpdateDTOs
{
    public class ChangeStatusDTO
    {
        public int Id { get; set; }
        public int StatusChange { get; set; }
        public string? RejectionReason { get; set; }
    }
}
