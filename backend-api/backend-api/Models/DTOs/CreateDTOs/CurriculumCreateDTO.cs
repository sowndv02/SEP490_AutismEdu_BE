using static backend_api.SD;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace backend_api.Models.DTOs.CreateDTOs
{
    public class CurriculumCreateDTO
    {
        [Required]
        public int AgeFrom { get; set; }
        [Required]
        public int AgeEnd { get; set; }
        [Required]
        public string Description { get; set; }
        public int? OriginalCurriculumId { get; set; }
    }
}
