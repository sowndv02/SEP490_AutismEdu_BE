using System.ComponentModel.DataAnnotations;

namespace AutismEduConnectSystem.DTOs.CreateDTOs
{
    public class ConverstationCreateDTO
    {
        [Required]
        public string ReceiverId { get; set; }
        public string Message { get; set; }
    }
}
