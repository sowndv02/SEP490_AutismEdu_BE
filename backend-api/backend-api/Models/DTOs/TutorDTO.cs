namespace backend_api.Models.DTOs
{
    public class TutorDTO
    {
        public string UserId { get; set; }
        public string FormalName { get; set; }
        public DateTime DateOfBirth { get; set; }
        public int StartAge { get; set; }
        public int EndAge { get; set; }
        public bool IsApprove { get; set; }
        public string? AboutMe { get; set; }
        public decimal? PriceFrom { get; set; }
        public decimal? PriceTo { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public string Email { get; set; }
        public string FullName { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Role { get; set; }
        public string? UserClaim { get; set; }
        public string? ImageUrl { get; set; }
        public string? ImageLocalPathUrl { get; set; }
        public string? ImageLocalUrl { get; set; }
        public IFormFile? Image { get; set; }
        public bool IsLockedOut { get; set; }
        public int UserClaimId { get; set; }
        public string UserType { get; set; }
    }
}
