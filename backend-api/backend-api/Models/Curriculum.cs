﻿using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using static backend_api.SD;

namespace backend_api.Models
{
    public class Curriculum
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public int AgeFrom { get; set; }
        public int AgeEnd { get; set; }
        public string Description { get; set; }
        public Status RequestStatus { get; set; } = Status.PENDING;
        public string? ApprovedId { get; set; }
        public string? RejectionReason { get; set; }
        [ForeignKey(nameof(ApprovedId))]
        public ApplicationUser? ApprovedBy { get; set; }
        public string? SubmiterId { get; set; }
        [ForeignKey(nameof(SubmiterId))]
        public Tutor? Submiter { get; set; }
        public int? TutorRegistrationRequestId { get; set; }
        public TutorRegistrationRequest? TutorRegistrationRequest { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime? UpdatedDate { get; set; }
    }
}