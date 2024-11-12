using static AutismEduConnectSystem.SD;
using System.ComponentModel.DataAnnotations.Schema;

namespace AutismEduConnectSystem.Models.DTOs.CreateDTOs
{
    public class ReportTutorCreateDTO
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public string TutorId { get; set; }
        public List<IFormFile> ReportMedias { get; set; }
    }
}
