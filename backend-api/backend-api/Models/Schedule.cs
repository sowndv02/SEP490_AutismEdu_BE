using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using static backend_api.SD;

namespace backend_api.Models
{
    public class Schedule
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public string TutorId { get; set; }
        public int StudentProfileId { get; set; }
        public int ScheduleTimeSlotId { get; set; }
        public DateTime ScheduleDate { get; set; }
        public TimeSpan Start { get; set; }
        public TimeSpan End { get; set; }
        public AttendanceStatus AttendanceStatus { get; set; }
        public PassingStatus PassingStatus { get; set; }
        public string Note { get; set; }
        public int ExerciseTypeId { get; set; }
        public int ExerciseId { get; set; }
        [ForeignKey(nameof(ExerciseId))]
        public Exercise Exercise { get; set; }
        [ForeignKey(nameof(ExerciseTypeId))]
        public ExerciseType ExerciseType { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime? UpdatedDate { get; set; }
        [ForeignKey(nameof(TutorId))]
        public Tutor Tutor { get; set; }
        [ForeignKey(nameof(StudentProfileId))]
        public StudentProfile StudentProfile { get; set; }
        [ForeignKey(nameof(ScheduleTimeSlotId))]
        public ScheduleTimeSlot ScheduleTimeSlot { get; set; }
    }
}
