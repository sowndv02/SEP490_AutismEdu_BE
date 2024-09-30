using backend_api.Models.DTOs.UpdateDTOs;
using System.ComponentModel.DataAnnotations;

namespace backend_api.Models.DTOs.CreateDTOs
{
    public class TutorCreateDTO
    {
        public TutorInfo TutorInfo { get; set; }
        public UserUpdateDTO TutorBasicInfo { get; set; }
        public List<WorkExperienceCreateDTO>? WorkExperiences { get; set; }
        public List<CertificateCreateDTO> Certificates { get; set; }
    }
}
