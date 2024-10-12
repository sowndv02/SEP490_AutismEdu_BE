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
        public int ChildId { get; set; }
        public int ScheduleTimeSlotId { get; set; }
        public DateTime ScheduleDate { get; set; }
        public AttendanceStatus AttendanceStatus { get; set; }
        public PassingStatus PassingStatus { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime? UpdatedDate { get; set; }
        [ForeignKey(nameof(TutorId))]
        public Tutor Tutor { get; set; }
        [ForeignKey(nameof(ChildId))]
        public ChildInformation Child { get; set; }
        [ForeignKey(nameof(ScheduleTimeSlotId))]
        public ScheduleTimeSlot ScheduleTimeSlot { get; set; }
    }
}
