using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend_api.Models
{
    public class Tutor
    {
        [Key]
        public string UserId { get; set; }
        public decimal? Price { get; set; }
        public int StartAge { get; set; }
        public int EndAge { get; set; }
        public DateTime DateOfBirth { get; set; }
        public string? AboutMe { get; set; }
        [ForeignKey(nameof(UserId))]
        public ApplicationUser User { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime? UpdatedDate { get; set; }
        public List<TutorRequest> Requests { get; set; }
        public List<Review> Reviews { get; set; }
        public List<Curriculum> Curriculums { get; set; }
        public List<Certificate> Certificates { get; set; }
        public List<WorkExperience> WorkExperiences { get; set; }
        public List<AvailableTimeSlot> AvailableTimeSlots { get; set; }
        [NotMapped]
        public decimal ReviewScore { get; set; }
        [NotMapped]
        public int TotalReview { get; set; }
    }
}
