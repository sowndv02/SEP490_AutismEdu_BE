using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using static AutismEduConnectSystem.SD;

namespace AutismEduConnectSystem.Models
{
    public class ExerciseType
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public string ExerciseTypeName { get; set; }
        public List<Exercise> Exercises { get; set; }
        public List<SyllabusExercise> SyllabusExercises { get; set; }
        public bool IsHide { get; set; } = true;
        public string SubmitterId { get; set; }
        [ForeignKey(nameof(SubmitterId))]
        public ApplicationUser Submitter { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime? UpdatedDate { get; set; }
    }
}
