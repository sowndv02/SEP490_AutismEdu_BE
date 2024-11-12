using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace AutismEduConnectSystem.Models.DTOs
{
    public class BlogDTO
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
        public string UrlImageDisplay { get; set; }
        public ApplicationUserDTO Author { get; set; }
        public int ViewCount { get; set; }
        public bool IsPublished { get; set; } = false;
        public DateTime PublishDate { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? UpdatedDate { get; set; }
    }
}
