using static backend_api.SD;

namespace backend_api.Models.DTOs.UpdateDTOs
{
    public class ScheduleUpdateDTO
    {
        public int Id { get; set; }
        public AttendanceStatus AttendanceStatus { get; set; }
        public PassingStatus PassingStatus { get; set; }
        public string Note { get; set; }
    }
}
