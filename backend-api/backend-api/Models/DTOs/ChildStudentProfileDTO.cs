using static backend_api.SD;

namespace backend_api.Models.DTOs
{
    public class ChildStudentProfileDTO
    {
        public int Id { get; set; }
        public string TutorName { get; set; }
        public string TutorPhoneNumber { get; set; }
        public string ChildName { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public StudentProfileStatus Status { get; set; }
    }
}
