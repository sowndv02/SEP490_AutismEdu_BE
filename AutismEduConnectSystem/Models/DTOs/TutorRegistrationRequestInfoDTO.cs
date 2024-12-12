using static AutismEduConnectSystem.SD;

namespace AutismEduConnectSystem.Models.DTOs
{
    public class TutorRegistrationRequestInfoDTO
    {
        public int Id { get; set; }
        public string Email { get; set; }
        public string FullName { get; set; }
        public string PhoneNumber { get; set; }
        public string? ImageUrl { get; set; }
        public string Address { get; set; }
        public DateTime DateOfBirth { get; set; }
        public int StartAge { get; set; }
        public int EndAge { get; set; }
        public decimal PriceFrom { get; set; }
        public decimal PriceEnd { get; set; }
        public float SessionHours { get; set; }
        public string? AboutMe { get; set; }
        public Status RequestStatus { get; set; }
        public string? RejectionReason { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? UpdatedDate { get; set; }
    }
}

