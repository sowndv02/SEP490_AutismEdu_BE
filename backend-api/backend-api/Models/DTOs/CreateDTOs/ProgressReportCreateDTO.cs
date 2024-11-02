using System.ComponentModel.DataAnnotations;

namespace backend_api.Models.DTOs.CreateDTOs
{
    public class ProgressReportCreateDTO
    {
        [Required(ErrorMessage = SD.ID_REQUIRED)]
        public int StudentProfileId { get; set; }
        [Required(ErrorMessage = SD.DATE_REQUIRED)]
        public DateTime From { get; set; }
        [Required(ErrorMessage = SD.DATE_REQUIRED)]
        public DateTime To { get; set; }
        public string? Achieved { get; set; }
        public string? Failed { get; set; }
        public string? NoteFromTutor { get; set; }
        [Required]
        public List<AssessmentResultCreateDTO> AssessmentResults { get; set; }
    }
}
