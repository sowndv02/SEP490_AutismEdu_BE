namespace AutismEduConnectSystem.Models.DTOs
{
    public class TutorDTO
    {
        public string UserId { get; set; }
        public DateTime DateOfBirth { get; set; }
        public int StartAge { get; set; }
        public int EndAge { get; set; }
        public string AboutMe { get; set; }
        public decimal PriceFrom { get; set; }
        public decimal PriceEnd { get; set; }
        public float SessionHours { get; set; }
        public string Email { get; set; }
        public string FullName { get; set; }
        public string? PhoneNumber { get; set; }
        public string? ImageUrl { get; set; }
        public int TotalReview { get; set; }
        public decimal ReviewScore { get; set; }
        public string Address { get; set; }
        public List<int> RejectChildIds {  get; set; }
        public List<CertificateDTO>? Certificates { get; set; }
        public List<WorkExperienceDTO>? WorkExperiences { get; set; }
        public List<CurriculumDTO>? Curriculums { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? UpdatedDate { get; set; }
    }
}
