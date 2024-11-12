using AutismEduConnectSystem.Models.DTOs.CreateDTOs;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using static AutismEduConnectSystem.SD;

namespace AutismEduConnectSystem.Models
{
    public class TutorRegistrationRequest
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        [Required]
        public string Email { get; set; }
        public string FullName { get; set; }
        [EmailAddress]
        public string PhoneNumber { get; set; }
        public string? ImageUrl { get; set; }
        [Required]
        public string Address { get; set; }
        [Required]
        public decimal PriceFrom { get; set; }
        [Required]
        public decimal PriceEnd { get; set; }
        [Required]
        public float SessionHours { get; set; }
        [Required]
        public DateTime DateOfBirth { get; set; }
        [Required]
        public int StartAge { get; set; }
        [Required]
        public int EndAge { get; set; }
        [Required]
        public string AboutMe { get; set; }
        public Status RequestStatus { get; set; } = Status.PENDING;
        public string? ApprovedId { get; set; }
        public string? RejectionReason { get; set; }
        [ForeignKey(nameof(ApprovedId))]
        public ApplicationUser? ApprovedBy { get; set; }
        public List<Curriculum>? Curriculums { get; set; }
        public List<WorkExperience>? WorkExperiences { get; set; }
        public List<Certificate> Certificates { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime? UpdatedDate { get; set; }

    }
}
