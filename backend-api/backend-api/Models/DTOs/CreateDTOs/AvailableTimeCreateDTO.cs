using System.ComponentModel.DataAnnotations;

namespace backend_api.Models.DTOs.CreateDTOs
{
    public class AvailableTimeCreateDTO
    {
        [Required(ErrorMessage = SD.WEEKDAY_REQUIRED)]
        public int Weekday { get; set; }
		[Required(ErrorMessage = SD.TIMESLOT_REQUIRED)]
        public AvailableTimeSlotCreateDTO TimeSlot { get; set; }
    }
}
