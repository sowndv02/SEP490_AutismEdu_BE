using System.ComponentModel.DataAnnotations;

namespace backend_api.Models.DTOs.UpdateDTOs
{
    public class BlogUpdateDTO
    {
        public int Id { get; set; }
        [Required(ErrorMessage = SD.TITLE_REQUIRED)]
        public string Title { get; set; }
        [Required(ErrorMessage = SD.CONTENT_REQUIRED)]
        public string Content { get; set; }
        public IFormFile ImageDisplay { get; set; }
    }
}
