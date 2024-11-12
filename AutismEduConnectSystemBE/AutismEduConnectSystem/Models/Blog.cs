using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AutismEduConnectSystem.Models
{
    public class Blog
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        [Required]
        public string Title { get; set; }
        [Required]
        public string Content { get; set; }
        public string UrlImageDisplay { get; set; }
        public string AuthorId { get; set; }
        [ForeignKey(nameof(AuthorId))]
        public ApplicationUser Author { get; set; }
        public int ViewCount { get; set; } = 0;
        public bool IsPublished { get; set; } = false;
        public DateTime PublishDate { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime? UpdatedDate { get; set; }
    }
}
