using System.ComponentModel.DataAnnotations;

namespace backend_api.Models.DTOs.CreateDTOs
{
    public class TutorCreateDTO
    {
        [Required]
        public string UserId { get; set; }
        public string FormalName { get; set; }
        public DateTime DateOfBirth { get; set; }
        public int StartAge { get; set; }
        public int EndAge { get; set; }
    }
}
