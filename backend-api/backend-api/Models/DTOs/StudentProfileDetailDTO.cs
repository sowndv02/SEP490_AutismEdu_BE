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
        public string? InitialCondition { get; set; }
        public SD.StudentProfileStatus Status { get; set; }
        public string Email { get; set; }
        public string FullName { get; set; }
        public string? PhoneNumber { get; set; }
        public string Address { get; set; }
        public string? ImageUrl { get; set; }      
        public List<InitialAssessmentResultDTO> InitialAssessmentResults { get; set; }
        public List<ScheduleTimeSlotDTO> ScheduleTimeSlots { get; set; }
    }
}
