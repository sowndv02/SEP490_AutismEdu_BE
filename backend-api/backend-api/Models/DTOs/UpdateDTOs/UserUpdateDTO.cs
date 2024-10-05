namespace backend_api.Models.DTOs.UpdateDTOs
{
    public class UserUpdateDTO
    {
        public string? PhoneNumber { get; set; }
        public string? FullName { get; set; }
        public string? Address { get; set; }
        public string? ImageUrl { get; set; }
        public IFormFile? Image { get; set; }
    }
}
