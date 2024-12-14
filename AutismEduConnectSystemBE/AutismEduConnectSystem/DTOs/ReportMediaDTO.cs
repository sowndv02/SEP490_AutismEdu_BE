using System.ComponentModel.DataAnnotations.Schema;

namespace AutismEduConnectSystem.DTOs
{
    public class ReportMediaDTO
    {
        public int Id { get; set; }
        public string UrlMedia { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? UpdatedDate { get; set; }
    }
}
