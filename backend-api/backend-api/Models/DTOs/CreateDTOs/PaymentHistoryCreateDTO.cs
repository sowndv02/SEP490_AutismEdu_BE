namespace backend_api.Models.DTOs.CreateDTOs
{
    public class PaymentHistoryCreateDTO
    {
        public int TransactionId { get; set; }
        public string Description { get; set; }
        public double Amount { get; set; }
        public DateTime PaymentDate { get; set; }
        public string BankTransactionId { get; set; }
        public string BankAccount { get; set; }
        public int PackagePaymentId { get; set; }
    }
}
