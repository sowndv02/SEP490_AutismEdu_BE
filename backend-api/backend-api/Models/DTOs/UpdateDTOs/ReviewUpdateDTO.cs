using Microsoft.AspNetCore.Mvc;

namespace backend_api.Models.DTOs.UpdateDTOs
{
    public class ReviewUpdateDTO
    {
        public decimal RateScore { get; set; }
        public string Description { get; set; }
    }
}
