using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend_api.Models
{
    public class ProgressReport
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public string TutorId { get; set; }
        public int ChildId { get; set; }
        public DateTime From { get; set; }
        public DateTime To { get; set; }
        public string? Achieved { get; set; }
        public string? Failed { get; set; }
        public string? NoteFromTutor { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime? UpdatedDate { get; set; }
        [ForeignKey(nameof(TutorId))]
        public ApplicationUser Tutor { get; set; }
        [ForeignKey(nameof(ChildId))]
        public ChildInformation Child { get; set; }
    }
}
