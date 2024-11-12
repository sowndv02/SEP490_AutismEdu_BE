using System.ComponentModel.DataAnnotations;

namespace AutismEduConnectSystem.Models.DTOs.CreateDTOs
{
    public class TestCreateDTO
    {
        [Required(ErrorMessage = SD.NAME_REQUIRED)]
        public string TestName { get; set; }
        [Required(ErrorMessage = SD.DESCRIPTION_REQUIRED)]
        public string TestDescription { get; set; }
    }
}
