namespace backend_api.Models.DTOs
{
    public class StudentProfileDetailDTO
    {
        public int Id { get; set; }
        public int ChildId { get; set; }
        public string? Name { get; set; }
        public string? StudentCode { get; set; }
        public bool isMale { get; set; }
        public DateTime? BirthDate { get; set; }
        public string? ImageUrlPath { get; set; }
        public string? InitialCondition { get; set; }
        public SD.StudentProfileStatus Status { get; set; }
        public TutorDTO Tutor { get; set; }
        public List<InitialAssessmentResultDTO> InitialAssessmentResults { get; set; }
        public List<ScheduleTimeSlotDTO> ScheduleTimeSlots { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.Now;
    }
}
