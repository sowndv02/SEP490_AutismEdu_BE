using AutismEduConnectSystem.Data;
using AutismEduConnectSystem.Models;
using AutismEduConnectSystem.Repository.IRepository;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace AutismEduConnectSystem.Repository
{
    public class PaymentHistoryRepository : Repository<PaymentHistory>, IPaymentHistoryRepository
    {
        private readonly ApplicationDbContext _context;
        public PaymentHistoryRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<int> GetTotalPaymentHistory(int packageId, Expression<Func<PaymentHistory, bool>>? filter = null)
        {
            IQueryable<PaymentHistory> query = _context.PaymentHistories.Where(x => x.PackagePaymentId == packageId).AsQueryable();
            if (filter != null)
                query = query.Where(filter);
            return await query.CountAsync();
        }

        public double GetTotalRevenues()
        {
            try
            {
                return _context.PaymentHistories.Sum(x => x.Amount);
                
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
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
