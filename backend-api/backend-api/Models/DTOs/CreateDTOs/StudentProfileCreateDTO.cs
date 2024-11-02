using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace backend_api.Models.DTOs.CreateDTOs
{
    public class StudentProfileCreateDTO
    {
        //Parent
        public string? Email { get; set; }
        public string? ParentFullName { get; set; }
        public string? Address { get; set; }
        public string? PhoneNumber { get; set; }

        //Child
        public string? ChildName { get; set; }
        public bool? isMale { get; set; }
        public DateTime? BirthDate { get; set; }
        public IFormFile? Media { get; set; }
        //TutorRequest <= 0 if none
        [Required(ErrorMessage = SD.ID)]
        public int TutorRequestId { get; set; }

        //Student profile ChildId 0 if create acc
        [Required(ErrorMessage = SD.ID)]
        public int ChildId { get; set; }
        [Required(ErrorMessage = SD.DESCRIPTION_REQUIRED)]
        public string InitialCondition { get; set; }
        public List<InitialAssessmentResultCreateDTO> InitialAssessmentResults { get; set; }
        public List<ScheduleTimeSlotCreateDTO> ScheduleTimeSlots { get; set; }
    }
}
