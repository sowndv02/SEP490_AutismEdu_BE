using System.ComponentModel.DataAnnotations;

namespace backend_api.Models.DTOs.CreateDTOs
{
    public class TutorInfo
    {
        [Required]
        public string FormalName { get; set; }
        
    }
}
