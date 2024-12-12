using System.ComponentModel.DataAnnotations;

namespace AutismEduConnectSystem.Models.DTOs.CreateDTOs
{
    public class BlogCreateDTO
    {
        [Required(ErrorMessage = SD.TITLE_REQUIRED)]
        public string Title { get; set; }
        [Required(ErrorMessage = SD.DESCRIPTION_REQUIRED)]
        public string Description { get; set; }
        [Required(ErrorMessage = SD.CONTENT_REQUIRED)]
        public string Content { get; set; }
        public IFormFile ImageDisplay { get; set; }
        public bool IsPublished { get; set; }
    }
}
