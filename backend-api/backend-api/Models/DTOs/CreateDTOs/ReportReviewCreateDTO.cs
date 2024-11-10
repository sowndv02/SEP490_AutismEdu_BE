using static backend_api.SD;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace backend_api.Models.DTOs.CreateDTOs
{
    public class ReportReviewCreateDTO
    {
        [Required(ErrorMessage = SD.DESCRIPTION_REQUIRED)]
        public string Description { get; set; }
        public int ReviewId { get; set; }
    }
}
