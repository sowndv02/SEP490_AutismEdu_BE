using AutismEduConnectSystem.DTOs;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AutismEduConnectSystem.Models
{
    public class Syllabus
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public int AgeFrom { get; set; }
        public int AgeEnd { get; set; }
        public List<SyllabusExercise> SyllabusExercises { get; set; }
        public bool IsDeleted { get; set; } = false;
        public string TutorId { get; set; }
        public Tutor Tutor { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime? UpdatedDate { get; set; }
        [NotMapped]
        public List<ExerciseTypeDTO> ExerciseTypes { get; set; }
    }
}
