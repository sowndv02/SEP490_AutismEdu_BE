using backend_api.Models;

namespace backend_api.Repository.IRepository
{
    public interface IPaymentHistoryRepository : IRepository<PaymentHistory>
    {
        Task<PaymentHistory> UpdateAsync(PaymentHistory model);
    }
}
