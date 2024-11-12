using AutismEduConnectSystem.Models;

namespace AutismEduConnectSystem.Repository.IRepository
{
    public interface IPaymentHistoryRepository : IRepository<PaymentHistory>
    {
        Task<PaymentHistory> UpdateAsync(PaymentHistory model);
    }
}
