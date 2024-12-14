using static AutismEduConnectSystem.SD;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace AutismEduConnectSystem.DTOs.CreateDTOs
{
    public class ReportReviewCreateDTO
    {
        [Required(ErrorMessage = DESCRIPTION_REQUIRED)]
        public string Description { get; set; }
        public int ReviewId { get; set; }
    }
}
