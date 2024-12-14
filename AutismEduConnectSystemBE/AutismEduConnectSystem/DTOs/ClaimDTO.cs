namespace AutismEduConnectSystem.DTOs
{
    public class ClaimDTO
    {
        public int Id { get; set; }
        public string ClaimType { get; set; }
        public string ClaimValue { get; set; }
        public List<ApplicationUserDTO> Users { get; set; }
        public int TotalUser { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime UpdatedDate { get; set; }
    }
}
