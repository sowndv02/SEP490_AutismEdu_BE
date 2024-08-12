namespace backend_api.Models.DTOs
{
    public class ApplicationUserDTO
    {
        public string Id { get; set; }
        public string Email { get; set; }
        public string FullName { get; set; }
        public string PhoneNumber { get; set; }
        public string? Role { get; set; }
        public string? UserClaim { get; set; }
        public string? ImageUrl { get; set; }
        public string? ImageLocalPathUrl { get; set; }
        public IFormFile? Image { get; set; }
        public DateTime? CreatedDate { get; set; }
        public bool IsLockedOut { get; set; }
    }
}
