﻿using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AutismEduConnectSystem.Models
{
    public class ScheduleTimeSlot
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public int Weekday { get; set; }
        public int StudentProfileId { get; set; }
        public TimeSpan From { get; set; }
        public TimeSpan To { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime? AppliedDate { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime? UpdatedDate { get; set; }
        [ForeignKey(nameof(StudentProfileId))]
        public StudentProfile StudentProfile { get; set; }
    }
}
