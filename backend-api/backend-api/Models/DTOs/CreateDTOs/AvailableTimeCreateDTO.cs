using System.ComponentModel.DataAnnotations;

namespace backend_api.Models.DTOs.CreateDTOs
{
    public class AvailableTimeCreateDTO
    {
        [Required]
        public int Weekday { get; set; }
		[Required]
        public AvailableTimeSlotCreateDTO TimeSlot { get; set; }
    }
}
