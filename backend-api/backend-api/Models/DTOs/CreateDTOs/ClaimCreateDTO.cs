namespace backend_api.Models.DTOs.CreateDTOs
{
    public class ClaimCreateDTO
    {
        public string ClaimType { get; set; }
        public string ClaimValue { get; set; }
        public string? UserId { get; set; }
    }
}
