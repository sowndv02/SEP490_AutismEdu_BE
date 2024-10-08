using System.ComponentModel.DataAnnotations;

namespace backend_api.Models.DTOs.CreateDTOs
{
    public class ProgressReportCreateDTO
    {
        [Required]
        public int ChildId { get; set; }
        [Required]
        public DateTime From { get; set; }
        [Required]
        public DateTime To { get; set; }
        public string? Achieved { get; set; }
        public string? Failed { get; set; }
        public string? NoteFromTutor { get; set; }
        [Required]
        public List<AssessmentResultCreateDTO> AssessmentResults { get; set; }
    }
}
