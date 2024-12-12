using System.ComponentModel.DataAnnotations;

namespace AutismEduConnectSystem.DTOs.UpdateDTOs
{
    public class ChildInformationUpdateDTO
    {
        [Required(ErrorMessage = SD.ID_REQUIRED)]
        public int ChildId { get; set; }
        public string? Name { get; set; }
        public bool isMale { get; set; }
        public DateTime? BirthDate { get; set; }
        public IFormFile? Media { get; set; }
    }
}
