﻿using static AutismEduConnectSystem.SD;

namespace AutismEduConnectSystem.DTOs
{
    public class CurriculumDTO
    {
        public int Id { get; set; }
        public int AgeFrom { get; set; }
        public int AgeEnd { get; set; }
        public string Description { get; set; }
        public Status RequestStatus { get; set; }
        public string? RejectionReason { get; set; }
        public ApplicationUserDTO? ApprovedBy { get; set; }
        public bool IsActive { get; set; }
        public bool IsDeleted { get; set; }
        public int VersionNumber { get; set; } = 1;
        public TutorUserInfo? Submitter { get; set; }
        public TutorRegistrationRequestInfoDTO? TutorRegistrationRequest { get; set; }
        public CurriculumDTO? OriginalCurriculum { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? UpdatedDate { get; set; }
    }
}
