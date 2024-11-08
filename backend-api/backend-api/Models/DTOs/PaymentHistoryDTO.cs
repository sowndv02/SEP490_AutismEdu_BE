namespace backend_api.Models.DTOs
{
    public class PaymentHistoryDTO
    {
        public int Id { get; set; }
        public int TransactionId { get; set; }
        public string Description { get; set; }
        public double Amount { get; set; }
        public DateTime PaymentDate { get; set; }
        public string BankTransactionId { get; set; }
        public string BankAccount { get; set; }
        public PackagePayment PackagePayment { get; set; }
        public ApplicationUser Submitter { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? UpdatedDate { get; set; }
    }
}
