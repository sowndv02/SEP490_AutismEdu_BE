using System.ComponentModel.DataAnnotations;

namespace backend_api.Models.DTOs.CreateDTOs
{
    public class BlogCreateDTO
    {
        [Required(ErrorMessage = SD.TITLE_REQUIRED)]
        public string Title { get; set; }
        [Required(ErrorMessage = SD.CONTENT_REQUIRED)]
        public string Content { get; set; }
        public IFormFile UrlImageDisplay { get; set; }
    }
}
