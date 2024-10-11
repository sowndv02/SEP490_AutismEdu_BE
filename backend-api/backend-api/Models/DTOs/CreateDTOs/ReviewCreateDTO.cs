using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend_api.Models.DTOs.CreateDTOs
{
    public class ReviewCreateDTO
    {
        public decimal RateScore { get; set; }
        public string Description { get; set; }
        public string ReviewerId { get; set; }
        public string RevieweeId { get; set; }
    }
}
