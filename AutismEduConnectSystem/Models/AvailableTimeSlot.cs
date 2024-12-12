using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AutismEduConnectSystem.Models
{
    public class AvailableTimeSlot
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public int Weekday { get; set; }
        public string TutorId { get; set; }
        public TimeSpan From { get; set; }
        public TimeSpan To { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime? UpdatedDate { get; set; }
        [ForeignKey(nameof(TutorId))]
        public Tutor Tutor { get; set; }
    }
}
