namespace AutismEduConnectSystem.DTOs
{
    public class GetAllStudentProfileTimeSlotDTO
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? StudentCode { get; set; }
        public List<ScheduleTimeSlotDTO> ScheduleTimeSlots { get; set; }

    }
}
