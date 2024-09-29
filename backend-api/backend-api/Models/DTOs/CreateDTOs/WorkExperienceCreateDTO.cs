namespace backend_api.Models.DTOs.CreateDTOs
{
    public class WorkExperienceCreateDTO
    {
        public string CompanyName { get; set; }
        public string Position { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }
}
