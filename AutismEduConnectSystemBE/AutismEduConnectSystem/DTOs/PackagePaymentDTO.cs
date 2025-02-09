﻿namespace AutismEduConnectSystem.DTOs
{
    public class PackagePaymentDTO
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public int Duration { get; set; }
        public string? Description { get; set; }
        public double Price { get; set; }
        public int VersionNumber { get; set; }
        public PackagePaymentDTO? Original { get; set; }
        public bool IsActive { get; set; }
        public bool IsHide { get; set; }
        public int TotalPurchases { get; set; }
        public ApplicationUserDTO Submitter { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? UpdatedDate { get; set; }
    }
}
