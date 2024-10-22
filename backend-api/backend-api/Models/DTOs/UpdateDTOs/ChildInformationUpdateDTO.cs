using System.ComponentModel.DataAnnotations;

namespace backend_api.Models.DTOs.UpdateDTOs
{
    public class ChildInformationUpdateDTO
    {
        [Required]
        public int ChildId { get; set; }
        public string? Name { get; set; }
        public bool isMale { get; set; }
        public DateTime? BirthDate { get; set; }
        public List<IFormFile> Medias { get; set; } = new List<IFormFile>();
    }
}
