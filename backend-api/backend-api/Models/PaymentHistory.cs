using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend_api.Models
{
    public class PaymentHistory
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public int TransactionId { get; set; }
        public string Description { get; set; }
        public double Amount { get; set; }
        public DateTime PaymentDate { get; set; }
        public string BankTransactionId { get; set; }
        public string BankAccount { get; set; }
        public int PackagePaymentId { get; set; }
        [ForeignKey(nameof(PackagePaymentId))]
        public PackagePayment PackagePayment { get; set; }
        public string SubmitterId { get; set; }
        [ForeignKey(nameof(SubmitterId))]
        public ApplicationUser Submitter { get; set; }
        public DateTime ExpirationDate { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime? UpdatedDate { get; set; }
    }
}
