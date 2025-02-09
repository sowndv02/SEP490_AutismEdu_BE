﻿using static AutismEduConnectSystem.SD;

namespace AutismEduConnectSystem.DTOs.CreateDTOs
{
    public class ReportTutorCreateDTO
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public string TutorId { get; set; }
        public int StudentProfileId { get; set; }
        public ReportTutorType ReportTutorType { get; set; }
        public List<IFormFile> ReportMedias { get; set; }
    }
}
