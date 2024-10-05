namespace backend_api.Models.DTOs.UpdateDTOs
{
    public class TutorRegistrationRequestChangeStatus
    {
        public int Id { get; set; }
        public int StatusChange { get; set; }
        public string? RejectionReason { get; set; }
    }
}
