﻿using System.ComponentModel.DataAnnotations;

namespace backend_api.Models.DTOs.CreateDTOs
{
    public class InitialAssessmentResultCreateDTO
    {
        [Required]
        public int OptionId { get; set; }
        [Required]
        public int QuestionId { get; set; }
    }
}
