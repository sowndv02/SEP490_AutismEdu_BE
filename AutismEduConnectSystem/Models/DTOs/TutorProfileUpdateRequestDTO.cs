using static AutismEduConnectSystem.SD;

namespace AutismEduConnectSystem.Models.DTOs
{
    public class TutorProfileUpdateRequestDTO
    {
        public int Id { get; set; }
        public string PhoneNumber { get; set; }
        public int StartAge { get; set; }
        public int EndAge { get; set; }
        public string Address { get; set; }
        public string AboutMe { get; set; }
        public decimal PriceFrom { get; set; }
        public decimal PriceEnd { get; set; }
        public float SessionHours { get; set; }
        public TutorDTO Tutor { get; set; }
        public Status RequestStatus { get; set; }
        public string? RejectionReason { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? UpdatedDate { get; set; }
    }
}
