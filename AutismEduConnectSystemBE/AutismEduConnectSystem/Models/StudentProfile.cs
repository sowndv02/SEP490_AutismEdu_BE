﻿using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using static AutismEduConnectSystem.SD;

namespace AutismEduConnectSystem.Models
{
    public class StudentProfile
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public string TutorId { get; set; }
        public int ChildId { get; set; }
        public string? StudentCode { get; set; }
        public string? InitialCondition { get; set; }
        public string? FinalCondition { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime? UpdatedDate { get; set; }
        public StudentProfileStatus Status { get; set; }
        [ForeignKey(nameof(TutorId))]
        public Tutor Tutor { get; set; }
        [ForeignKey(nameof(ChildId))]
        public ChildInformation Child { get; set; }
        public List<InitialAssessmentResult> InitialAndFinalAssessmentResults { get; set; }
        public List<ScheduleTimeSlot> ScheduleTimeSlots { get; set; }
    }
}
