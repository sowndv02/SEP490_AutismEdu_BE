using System.ComponentModel.DataAnnotations.Schema;

namespace AutismEduConnectSystem.Models.DTOs
{
    public class TestDTO
    {
        public int Id { get; set; }
        public string TestName { get; set; }
        public string TestDescription { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime? UpdatedDate { get; set; }
        public List<AssessmentQuestionDTO> Questions { get; set; }
    }
}
