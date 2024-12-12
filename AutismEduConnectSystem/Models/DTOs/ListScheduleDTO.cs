namespace AutismEduConnectSystem.Models.DTOs
{
    public class ListScheduleDTO
    {
        public DateTime MaxDate { get; set; }
        public List<ScheduleDTO> Schedules { get; set; }
    }
}
