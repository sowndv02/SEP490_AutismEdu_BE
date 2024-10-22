using backend_api.Models.DTOs.UpdateDTOs;
using System.ComponentModel.DataAnnotations;

namespace backend_api.Models.DTOs.CreateDTOs
{
    public class TutorRegistrationRequestCreateDTO
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }
        [Required]
        public string FullName { get; set; }
        [Required]
        public string PhoneNumber { get; set; }
        [Required]
        public string Address { get; set; }
        [Required]
        public decimal PriceFrom { get; set; }
        [Required]
        public decimal PriceEnd { get; set; }
        [Required]
        public float SessionHours { get; set; }
        public IFormFile Image { get; set; }
        [Required]
        public DateTime DateOfBirth { get; set; }
        [Required]
        public int StartAge { get; set; }
        [Required]
        public int EndAge { get; set; }
        public List<CurriculumCreateDTO>? Curriculums { get; set; }
        [Required]
        public string AboutMe { get; set; }
        public List<WorkExperienceCreateDTO>? WorkExperiences { get; set; }
        public List<CertificateCreateDTO> Certificates { get; set; }
    }
}
