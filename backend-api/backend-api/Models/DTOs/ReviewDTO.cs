using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend_api.Models.DTOs
{
    public class ReviewDTO
    {
        public int Id { get; set; }
        public decimal RateScore { get; set; }
        public string Description { get; set; }
        public string ReviewerId { get; set; }
        public string RevieweeId { get; set; }
    }
}
