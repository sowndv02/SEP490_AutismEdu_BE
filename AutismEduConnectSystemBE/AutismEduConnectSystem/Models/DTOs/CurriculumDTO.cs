using static AutismEduConnectSystem.SD;

namespace AutismEduConnectSystem.Models.DTOs
{
    public class CurriculumDTO
    {
        public int Id { get; set; }
        public int AgeFrom { get; set; }
        public int AgeEnd { get; set; }
        public string Description { get; set; }
        public Status RequestStatus { get; set; }
        public string? RejectionReason { get; set; }
        public ApplicationUserDTO? ApprovedBy { get; set; }
        public bool IsActive { get; set; } = false;
        public int VersionNumber { get; set; } = 1;
        public TutorInfoDTO? Submitter { get; set; }
        public string OriginalDescription { get; set; }
        public int OriginalAgeFrom { get; set; }
        public int OriginalAgeEnd { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime? UpdatedDate { get; set; }
    }
}
