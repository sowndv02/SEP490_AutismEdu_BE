namespace backend_api.Models.DTOs.CreateDTOs
{
    public class AvailableTimeCreateDTO
    {
        public int Weekday { get; set; }
        public List<string> Times { get; set; }
    }
}
