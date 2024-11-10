using System.ComponentModel.DataAnnotations.Schema;

namespace backend_api.Models.DTOs
{
    public class ReportMediaDTO
    {
        public int Id { get; set; }
        public string UrlMedia { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? UpdatedDate { get; set; }
    }
}
