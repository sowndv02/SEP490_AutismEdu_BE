using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend_api.Models
{
    public class Exercise
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public string ExerciseName { get; set; }
        public int ExerciseTypeId { get; set; }
        public string Description { get; set; }
        public bool IsDeleted { get; set; } = false;
        public bool IsActive { get; set; } = false;
        public string TutorId { get; set; }
        public int VersionNumber { get; set; } = 1;
        public int? OriginalId { get; set; }
        [ForeignKey(nameof(OriginalId))]
        public Exercise? Original { get; set; }
        [ForeignKey(nameof(ExerciseTypeId))]
        public ExerciseType ExerciseType { get; set; }
        public List<SyllabusExercise> SyllabusExercises { get; set; }

        [ForeignKey(nameof(TutorId))]
        public Tutor Tutor { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime? UpdatedDate { get; set; }
    }
}
