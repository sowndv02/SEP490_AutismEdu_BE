using System.ComponentModel.DataAnnotations;

namespace AutismEduConnectSystem.DTOs.CreateDTOs
{
    public class WorkExperienceCreateDTO
    {
        [Required(ErrorMessage = SD.NAME_REQUIRED)]
        public string CompanyName { get; set; }
        [Required(ErrorMessage = SD.POSITION_REQUIRED)]
        public string Position { get; set; }
        [Required(ErrorMessage = SD.DATE_REQUIRED)]
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int? OriginalId { get; set; }
    }
}
