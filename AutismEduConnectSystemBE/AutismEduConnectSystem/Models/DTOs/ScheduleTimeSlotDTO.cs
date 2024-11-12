namespace AutismEduConnectSystem.Models.DTOs
{
    public class ScheduleTimeSlotDTO
    {
        public int Id { get; set; }
        public int Weekday { get; set; }
        public TimeSpan From { get; set; }
        public TimeSpan To { get; set; }
    }
}
