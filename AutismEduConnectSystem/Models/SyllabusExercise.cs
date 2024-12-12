using System.ComponentModel.DataAnnotations.Schema;

namespace AutismEduConnectSystem.Models
{
    public class SyllabusExercise
    {
        public int SyllabusId { get; set; }
        [ForeignKey(nameof(SyllabusId))]
        public Syllabus Syllabus { get; set; }

        public int ExerciseTypeId { get; set; }
        [ForeignKey(nameof(ExerciseTypeId))]
        public ExerciseType ExerciseType { get; set; }

        public int ExerciseId { get; set; }
        [ForeignKey(nameof(ExerciseId))]
        public Exercise Exercise { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;
    }
}
