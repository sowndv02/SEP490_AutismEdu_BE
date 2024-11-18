using System.ComponentModel.DataAnnotations;

namespace AutismEduConnectSystem.Models.DTOs.CreateDTOs
{
    public class ChildInformationCreateDTO
    {
        [Required(ErrorMessage = SD.NAME_REQUIRED)]
        public string Name { get; set; }
        [Required(ErrorMessage = SD.GENDER_REQUIRED)]
        public bool isMale { get; set; }
        [Required(ErrorMessage = SD.BIRTH_DATE_REQUIRED)]
        public DateTime BirthDate { get; set; }
        public IFormFile? Media { get; set; }
    }
}
