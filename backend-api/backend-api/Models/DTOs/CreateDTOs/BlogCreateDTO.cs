using System.ComponentModel.DataAnnotations;

namespace backend_api.Models.DTOs.CreateDTOs
{
    public class BlogCreateDTO
    {
        [Required]
        public string Title { get; set; }
        [Required]
        public string Content { get; set; }
        public IFormFile UrlImageDisplay { get; set; }
    }
}
