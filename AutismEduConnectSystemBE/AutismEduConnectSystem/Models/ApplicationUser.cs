﻿using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AutismEduConnectSystem.Models
{
    public class ApplicationUser : IdentityUser
    {
        [Required]
        public string FullName { get; set; }
        public DateTime CreatedDate { get; set; }
        public string? ImageUrl { get; set; }
        [NotMapped]
        public string RoleId { get; set; }
        [NotMapped]
        public string Role { get; set; }
        [NotMapped]
        public string UserClaim { get; set; }
        [NotMapped]
        public bool IsLockedOut { get; set; }
        public string UserType { get; set; } = SD.APPLICATION_USER;
        public List<TutorRequest> TutorRequests { get; set; }
        public string? Address { get; set; }
        public Tutor? TutorProfile { get; set; }
    }
}
