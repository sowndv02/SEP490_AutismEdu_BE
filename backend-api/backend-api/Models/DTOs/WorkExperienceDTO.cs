using static backend_api.SD;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend_api.Models.DTOs
{
    public class WorkExperienceDTO
    {
        public int Id { get; set; }
        public ApplicationUserDTO? Submiter { get; set; }
        public string CompanyName { get; set; }
        public string Position { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public Status RequestStatus { get; set; }
        public string? RejectionReason { get; set; }
        public ApplicationUserDTO? ApprovedBy { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime UpdatedDate { get; set; }
    }
}
