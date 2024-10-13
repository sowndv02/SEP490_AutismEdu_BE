using static backend_api.SD;

namespace backend_api.Models.DTOs
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
        public string OrifinalDescription { get; set; }
        public int OrifinalAgeFrom { get; set; }
        public int OrifinalAgeEnd { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime? UpdatedDate { get; set; }
    }
}
