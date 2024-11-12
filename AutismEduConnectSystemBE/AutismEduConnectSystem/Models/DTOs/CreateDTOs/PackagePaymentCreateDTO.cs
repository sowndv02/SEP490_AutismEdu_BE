using System.ComponentModel.DataAnnotations;

namespace AutismEduConnectSystem.Models.DTOs.CreateDTOs
{
    public class PackagePaymentCreateDTO
    {
        [Required(ErrorMessage = SD.TITLE_REQUIRED)]
        public string Title { get; set; }
        public int Duration { get; set; }
        public string? Description { get; set; }
        public double Price { get; set; }
        public bool IsHide { get; set; }
        public int? OriginalId { get; set; }
    }
}
