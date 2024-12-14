using static AutismEduConnectSystem.SD;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace AutismEduConnectSystem.DTOs.CreateDTOs
{
    public class CurriculumCreateDTO
    {
        [Required(ErrorMessage = AGE_REQUIRED)]
        public int AgeFrom { get; set; }
        [Required(ErrorMessage = AGE_REQUIRED)]
        public int AgeEnd { get; set; }
        [Required(ErrorMessage = DESCRIPTION_REQUIRED)]
        public string Description { get; set; }
        public int? OriginalCurriculumId { get; set; }
    }
}
