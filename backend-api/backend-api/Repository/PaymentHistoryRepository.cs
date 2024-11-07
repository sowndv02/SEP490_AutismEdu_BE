using backend_api.Data;
using backend_api.Models;
using backend_api.Repository.IRepository;

namespace backend_api.Repository
{
    public class PaymentHistoryRepository : Repository<PaymentHistory>, IPaymentHistoryRepository
    {
        private readonly ApplicationDbContext _context;
        public PaymentHistoryRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<PaymentHistory> UpdateAsync(PaymentHistory model)
        {
            try
            {
                _context.PaymentHistories.Update(model);
                await _context.SaveChangesAsync();
                return model;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
    }
}
