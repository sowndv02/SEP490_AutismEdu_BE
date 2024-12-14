using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AutismEduConnectSystem.Models
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
        public string TutorId { get; set; }
        [ForeignKey(nameof(ExerciseTypeId))]
        public ExerciseType ExerciseType { get; set; }
        public List<SyllabusExercise> SyllabusExercises { get; set; }

        [ForeignKey(nameof(TutorId))]
        public Tutor Tutor { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime? UpdatedDate { get; set; }
    }
}
