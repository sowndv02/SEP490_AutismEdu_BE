using System.ComponentModel.DataAnnotations;

namespace backend_api.Models.DTOs.CreateDTOs
{
    public class PackagePaymentCreateDTO
    {
        [Required(ErrorMessage = SD.TITLE_REQUIRED)]
        public string Title { get; set; }
        public int Duration { get; set; }
        public string? Description { get; set; }
        public double Price { get; set; }
        public bool IsActive { get; set; }
        public int? OriginalId { get; set; }
    }
}
