using System.ComponentModel.DataAnnotations;

namespace AutismEduConnectSystem.Models.DTOs.CreateDTOs
{
    public class ConverstationCreateDTO
    {
        [Required]
        public string ReceiverId { get; set; }
    }
}
