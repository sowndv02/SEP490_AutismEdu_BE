using System.ComponentModel.DataAnnotations;

namespace AutismEduConnectSystem.DTOs.CreateDTOs
{
    public class AvailableTimeSlotCreateDTO
    {
        [Required(ErrorMessage = SD.WEEKDAY_REQUIRED)]
        public int Weekday { get; set; }
        [Required(ErrorMessage = SD.TIMESLOT_REQUIRED)]
        public string From { get; set; }
        [Required(ErrorMessage = SD.TIMESLOT_REQUIRED)]
        public string To { get; set; }
    }
}
