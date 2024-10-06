using System.ComponentModel.DataAnnotations.Schema;

namespace backend_api.Models.DTOs.CreateDTOs
{
    public class TutorRequestCreateDTO
    {
        public string TutorId { get; set; }
        public int ChildId { get; set; }
        public string? Description { get; set; }
    }
}
