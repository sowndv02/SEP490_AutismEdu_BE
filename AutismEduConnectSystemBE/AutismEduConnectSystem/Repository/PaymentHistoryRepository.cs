using AutismEduConnectSystem.Data;
using AutismEduConnectSystem.Models;
using AutismEduConnectSystem.Repository.IRepository;

namespace AutismEduConnectSystem.Repository
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
