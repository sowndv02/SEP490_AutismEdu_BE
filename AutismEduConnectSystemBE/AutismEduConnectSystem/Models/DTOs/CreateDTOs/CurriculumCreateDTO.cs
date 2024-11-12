using static AutismEduConnectSystem.SD;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace AutismEduConnectSystem.Models.DTOs.CreateDTOs
{
    public class CurriculumCreateDTO
    {
        [Required(ErrorMessage = SD.AGE_REQUIRED)]
        public int AgeFrom { get; set; }
        [Required(ErrorMessage = SD.AGE_REQUIRED)]
        public int AgeEnd { get; set; }
        [Required(ErrorMessage = SD.DESCRIPTION_REQUIRED)]
        public string Description { get; set; }
        public int? OriginalCurriculumId { get; set; }
    }
}
