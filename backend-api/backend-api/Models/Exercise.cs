using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend_api.Models
{
    public class Exercise
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ExerciseId { get; set; }
        public string ExerciseName { get; set; }
        public int ExerciseTypeId { get; set; }
        public string ExerciseContent { get; set; }
        public string TutorId { get; set; }

        [ForeignKey(nameof(ExerciseTypeId))]
        public ExerciseType ExerciseType { get; set; }

        [ForeignKey(nameof(TutorId))]
        public Tutor Tutor { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime? UpdatedDate { get; set; }
    }
}
