using System.ComponentModel.DataAnnotations.Schema;

namespace AutismEduConnectSystem.Models
{
    public class NotificationDTO
    {
        public int Id { get; set; }
        public string Message { get; set; }
        public string UrlDetail { get; set; }
        public bool IsRead { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}
