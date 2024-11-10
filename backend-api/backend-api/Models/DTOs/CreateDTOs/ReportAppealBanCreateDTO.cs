using static backend_api.SD;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend_api.Models.DTOs.CreateDTOs
{
    public class ReportAppealBanCreateDTO
    {
        public string Title { get; set; }
        public string Email { get; set; }
        public string Description { get; set; }
    }
}
