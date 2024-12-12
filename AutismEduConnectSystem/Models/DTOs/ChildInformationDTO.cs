namespace AutismEduConnectSystem.Models.DTOs
{
    public class ChildInformationDTO
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? Gender { get; set; }
        public DateTime? BirthDate { get; set; }
        public string? ParentPhoneNumber { get; set; }
        public string? Address { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime? UpdatedDate { get; set; }
        public string? ImageUrlPath { get; set; }
    }
}
