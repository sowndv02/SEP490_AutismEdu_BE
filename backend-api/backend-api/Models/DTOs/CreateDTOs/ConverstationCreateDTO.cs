using System.ComponentModel.DataAnnotations;

namespace backend_api.Models.DTOs.CreateDTOs
{
    public class ConverstationCreateDTO
    {
        [Required]
        public string ReceiverId { get; set; }
    }
}
