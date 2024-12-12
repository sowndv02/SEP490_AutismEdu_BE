using AutismEduConnectSystem.Models.DTOs.UpdateDTOs;
using System.ComponentModel.DataAnnotations;

namespace AutismEduConnectSystem.Models.DTOs.CreateDTOs
{
    public class TutorRegistrationRequestCreateDTO
    {
        [Required(ErrorMessage = SD.EMAIL_REQUIRED)]
        [EmailAddress]
        public string Email { get; set; }
        [Required(ErrorMessage = SD.NAME_REQUIRED)]
        public string FullName { get; set; }
        [Required(ErrorMessage = SD.PHONE_NUMBER_REQUIRED)]
        public string PhoneNumber { get; set; }
        [Required(ErrorMessage = SD.ADDRESS_REQUIRED)]
        public string Address { get; set; }
        [Required(ErrorMessage = SD.PRICE_REQUIRED)]
        public decimal PriceFrom { get; set; }
        [Required(ErrorMessage = SD.PRICE_REQUIRED)]
        public decimal PriceEnd { get; set; }
        [Required(ErrorMessage = SD.SESSION_HOUR_REQUIRED)]
        public float SessionHours { get; set; }
        public IFormFile Image { get; set; }
        [Required(ErrorMessage = SD.BIRTH_DATE_REQUIRED)]
        public DateTime DateOfBirth { get; set; }
        [Required(ErrorMessage = SD.AGE_REQUIRED)]
        public int StartAge { get; set; }
        [Required(ErrorMessage = SD.AGE_REQUIRED)]
        public int EndAge { get; set; }
        public List<CurriculumCreateDTO>? Curriculums { get; set; }
        [Required(ErrorMessage = SD.DESCRIPTION_REQUIRED)]
        public string AboutMe { get; set; }
        public List<WorkExperienceCreateDTO>? WorkExperiences { get; set; }
        public List<CertificateCreateDTO> Certificates { get; set; }
    }
}
