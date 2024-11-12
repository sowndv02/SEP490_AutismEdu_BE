using System.ComponentModel.DataAnnotations;

namespace AutismEduConnectSystem.Models.DTOs.CreateDTOs
{
    public class RoleCreateDTO
    {
        [Required(ErrorMessage = SD.NAME_REQUIRED)]
        public string Name { get; set; }
    }
}
