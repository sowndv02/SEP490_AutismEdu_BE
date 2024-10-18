using System.ComponentModel.DataAnnotations;

namespace backend_api.Models.DTOs.CreateDTOs
{
    public class ScheduleTimeSlotCreateDTO
    {
        [Required]
        public int Weekday { get; set; }
        [Required]
        public string From { get; set; }
        [Required]
        public string To { get; set; }
    }
}
