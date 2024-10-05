namespace backend_api.Models.DTOs
{
    public class AvailableTimeDTO
    {
        public int Id { get; set; }
        public string Weekday { get; set; }
        public List<AvailableTimeSlotDTO> Times { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime? UpdatedDate { get; set; }
    }
}
