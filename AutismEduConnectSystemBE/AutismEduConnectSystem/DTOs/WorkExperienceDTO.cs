﻿using static AutismEduConnectSystem.SD;
using System.ComponentModel.DataAnnotations.Schema;

namespace AutismEduConnectSystem.DTOs
{
    public class WorkExperienceDTO
    {
        public int Id { get; set; }
        public string CompanyName { get; set; }
        public string Position { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public Status RequestStatus { get; set; }
        public bool IsDeleted { get; set; }
        public string? RejectionReason { get; set; }
        public TutorUserInfo? Submitter { get; set; }
        public TutorRegistrationRequestInfoDTO? TutorRegistrationRequest { get; set; }
        public WorkExperienceDTO? OriginalWorkExperience { get; set; }
        public ApplicationUserDTO? ApprovedBy { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime UpdatedDate { get; set; }
    }
}
