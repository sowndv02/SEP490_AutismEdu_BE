using static backend_api.SD;

namespace backend_api.Models.DTOs
{
    public class ScheduleDTO
    {
        public int Id { get; set; }
        public DateTime ScheduleDate { get; set; }
        public TimeSpan Start { get; set; }
        public TimeSpan End { get; set; }
        public AttendanceStatus AttendanceStatus { get; set; }
        public PassingStatus PassingStatus { get; set; }
        public int? SyllabusId { get; set; }
        public ExerciseDTO? Exercise { get; set; }
        public ExerciseTypeInfoDTO? ExerciseType { get; set; }
        public string Note { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime? UpdatedDate { get; set; }
        public StudentProfileDTO? StudentProfile { get; set; }
    }
}
