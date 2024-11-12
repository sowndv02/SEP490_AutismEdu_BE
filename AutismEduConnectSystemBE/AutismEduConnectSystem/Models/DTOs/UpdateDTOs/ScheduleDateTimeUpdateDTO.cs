namespace AutismEduConnectSystem.Models.DTOs.UpdateDTOs
{
    public class ScheduleDateTimeUpdateDTO
    {
        public int Id { get; set; }
        public DateTime ScheduleDate { get; set; }
        public TimeSpan Start { get; set; }
        public TimeSpan End { get; set; }
    }
}
