﻿using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend_api.Models
{
    public class AssessmentResult
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public int OptionId { get; set; }
        public int ProgressReportId { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime? UpdatedDate { get; set; }
        [ForeignKey(nameof(OptionId))]
        public AssessmentOption Option { get; set; }
        [ForeignKey(nameof(ProgressReportId))]
        public ProgressReport ProgressReport { get; set; }
    }
}
