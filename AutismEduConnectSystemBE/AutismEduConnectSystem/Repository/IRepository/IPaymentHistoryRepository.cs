using AutismEduConnectSystem.Models;
using System.Linq.Expressions;

namespace AutismEduConnectSystem.Repository.IRepository
{
    public interface IPaymentHistoryRepository : IRepository<PaymentHistory>
    {
        Task<PaymentHistory> UpdateAsync(PaymentHistory model);
        Task<int> GetTotalPaymentHistory(int packageId,Expression<Func<PaymentHistory, bool>>? filter = null);
        double GetTotalRevenues();
    }
}
