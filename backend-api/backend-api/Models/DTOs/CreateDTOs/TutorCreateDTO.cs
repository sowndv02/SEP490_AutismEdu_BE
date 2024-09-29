using System.ComponentModel.DataAnnotations;

namespace backend_api.Models.DTOs.CreateDTOs
{
    public class TutorCreateDTO
    {
        [Required]
        public string FormalName { get; set; }
        [Required]
        public DateTime DateOfBirth { get; set; }
        [Required]
        public int StartAge { get; set; }
        [Required]
        public int EndAge { get; set; }
    }
}
