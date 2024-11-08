namespace backend_api.Models.DTOs
{
    public class PackagePaymentDTO
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public int Duration { get; set; }
        public string? Description { get; set; }
        public double Price { get; set; }
        public int VersionNumber { get; set; }
        public PackagePayment? Original { get; set; }
        public bool IsActive { get; set; }
        public bool IsDeleted { get; set; }
        public ApplicationUser Submitter { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? UpdatedDate { get; set; }
    }
}
