using System.ComponentModel.DataAnnotations;

namespace backend_api.Models.DTOs.CreateDTOs
{
    public class AvailableTimeSlotCreateDTO
    {
        [Required]
        public TimeSpan From { get; set; }
        [Required]
        public TimeSpan To { get; set; }
    }
}
