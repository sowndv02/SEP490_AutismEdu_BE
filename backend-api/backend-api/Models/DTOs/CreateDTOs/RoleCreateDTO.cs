using System.ComponentModel.DataAnnotations;

namespace backend_api.Models.DTOs.CreateDTOs
{
    public class RoleCreateDTO
    {
        [Required]
        public string Name { get; set; }
    }
}
