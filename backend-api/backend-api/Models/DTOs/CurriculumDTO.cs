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
        public TutorDTO? Submiter { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime? UpdatedDate { get; set; }
    }
}
