namespace AutismEduConnectSystem.Models.DTOs
{
    public class StudentProfileDetailParentDTO
    {
        public int Id { get; set; }
        public int ChildId { get; set; }
        public string? Name { get; set; }
        public string? StudentCode { get; set; }
        public bool isMale { get; set; }
        public DateTime? BirthDate { get; set; }
        public string? ImageUrlPath { get; set; }
        public SD.StudentProfileStatus Status { get; set; }
        public TutorDTO Tutor { get; set; }
        public AssessmentDTO InitialAssessmentResults { get; set; }
        public AssessmentDTO FinalAssessmentResults { get; set; }
        public List<ScheduleTimeSlotDTO> ScheduleTimeSlots { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime? UpdatedDate { get; set; }
    }
}
