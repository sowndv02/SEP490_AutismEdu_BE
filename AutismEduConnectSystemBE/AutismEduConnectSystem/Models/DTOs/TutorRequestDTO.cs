using static AutismEduConnectSystem.SD;

namespace AutismEduConnectSystem.Models.DTOs
{
    public class TutorRequestDTO
    {
        public int Id { get; set; }
        public ChildInformationDTO ChildInformation { get; set; }
        public ApplicationUserDTO Parent { get; set; }
        public TutorDTO? Tutor { get; set; }
        public string? Description { get; set; }
        public Status RequestStatus { get; set; }
        public RejectType RejectType { get; set; }
        public string? RejectionReason { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime? UpdatedDate { get; set; }
    }
}
