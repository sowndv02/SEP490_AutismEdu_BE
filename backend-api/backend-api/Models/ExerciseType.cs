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
        public int AgeFrom { get; set; }
        public int AgeTo { get; set; }
        public Status RequestStatus { get; set; } = Status.PENDING;
        public string? TutorId { get; set; }

        [ForeignKey(nameof(TutorId))]
        public Tutor? Tutor { get; set; }
    }
}
