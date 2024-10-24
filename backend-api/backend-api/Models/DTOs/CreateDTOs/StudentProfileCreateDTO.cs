﻿using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace backend_api.Models.DTOs.CreateDTOs
{
    public class StudentProfileCreateDTO
    {
        //Parent
        public string? Email { get; set; }
        public string? ParentFullName { get; set; }
        public string? Address { get; set; }
        public string? PhoneNumber { get; set; }

        //Child
        public string? ChildName { get; set; }
        public bool? isMale { get; set; }
        public DateTime? BirthDate { get; set; }
        public IFormFile? Media { get; set; }

        //Student profile
        [Required]
        public int ChildId { get; set; }
        [Required]
        public string InitialCondition { get; set; }
        public List<InitialAssessmentResultCreateDTO> InitialAssessmentResults { get; set; }
        public List<ScheduleTimeSlotCreateDTO> ScheduleTimeSlots { get; set; }
    }
}
