namespace backend_api.Models.DTOs
{
    public class StudentProfileDetailTutorDTO
    {
        public int Id { get; set; }
        public string TutorId { get; set; }
        public int ChildId { get; set; }
        public string? Name { get; set; }
        public string? StudentCode { get; set; }
        public bool isMale { get; set; }
        public DateTime? BirthDate { get; set; }
        public string? ImageUrlPath { get; set; }
        public string? Address { get; set; }
        public string? PhoneNumber { get; set; }
        public SD.StudentProfileStatus Status { get; set; }
        public AssessmentDTO InitialAssessmentResults { get; set; }
        public AssessmentDTO FinalAssessmentResults { get; set; }
        public List<ScheduleTimeSlotDTO> ScheduleTimeSlots { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime? UpdatedDate { get; set; }
    }
}
