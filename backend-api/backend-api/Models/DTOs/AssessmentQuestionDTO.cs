﻿namespace backend_api.Models.DTOs
{
    public class AssessmentQuestionDTO
    {
        public int Id { get; set; }
        public string? Question { get; set; }
        public DateTime CreatedDate { get; set; }
        public List<AssessmentOptionDTO> Options { get; set; }
    }
}