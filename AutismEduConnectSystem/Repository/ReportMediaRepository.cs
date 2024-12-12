using AutismEduConnectSystem.Data;
using AutismEduConnectSystem.Models;
using AutismEduConnectSystem.Repository.IRepository;

namespace AutismEduConnectSystem.Repository
{
    public class ReportMediaRepository : Repository<ReportMedia>, IReportMediaRepository
    {
        private readonly ApplicationDbContext _context;
        public ReportMediaRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<ReportMedia> UpdateAsync(ReportMedia model)
        {
            try
            {
                _context.ReportMedias.Update(model);
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
