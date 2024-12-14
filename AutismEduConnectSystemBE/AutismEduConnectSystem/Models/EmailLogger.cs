using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AutismEduConnectSystem.Models
{
    public class EmailLogger
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public string Email { get; set; }
        public string? UserId { get; set; }
        [ForeignKey(nameof(UserId))]
        public ApplicationUser User { get; set; }
        public string Subject { get; set; }
        public string Message { get; set; }
        public int MaxRetries { get; set; } = 0;
        public bool SendFirstTime { get; set; } = true;
        public string? ErrorCode { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.Now;

    }
}
