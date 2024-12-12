using System.ComponentModel.DataAnnotations;

namespace AutismEduConnectSystem.DTOs.UpdateDTOs
{
    public class ScheduleTimeSlotUpdateDTO
    {
        [Required(ErrorMessage = SD.ID_REQUIRED)]
        public int TimeSlotId { get; set; }
        [Required(ErrorMessage = SD.WEEKDAY_REQUIRED)]
        public int Weekday { get; set; }
        [Required(ErrorMessage = SD.TIMESLOT_REQUIRED)]
        public string From { get; set; }
        [Required(ErrorMessage = SD.TIMESLOT_REQUIRED)]
        public string To { get; set; }
    }
}
