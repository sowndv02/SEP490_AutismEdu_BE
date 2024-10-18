using System.ComponentModel.DataAnnotations;

namespace backend_api.Models.DTOs.CreateDTOs
{
    public class StudentProfileCreateDTO
    {
        [Required]
        public int ChildId { get; set; }
        [Required]
        public string InitialCondition { get; set; }
        [Required]
        public int TutorRequestId { get; set; } = 0;
        public List<InitialAssessmentResultCreateDTO> InitialAssessmentResults { get; set; }
        public List<ScheduleTimeSlotCreateDTO> ScheduleTimeSlots { get; set; }
    }
}
