using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using static backend_api.SD;

namespace backend_api.Models
{
    public class ExerciseType
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public string ExerciseTypeName { get; set; }
        public int VersionNumber { get; set; } = 1;
        public int? OriginalId { get; set; }
        [ForeignKey(nameof(OriginalId))]
        public ExerciseType? Original { get; set; }
        public bool IsDeleted { get; set; } = false;
        public bool IsActive { get; set; } = false;
        public string SubmitterId { get; set; }
        public List<Exercise> Exercises { get; set; }
        [ForeignKey(nameof(SubmitterId))]
        public ApplicationUser Submitter { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime? UpdatedDate { get; set; }
    }
}
