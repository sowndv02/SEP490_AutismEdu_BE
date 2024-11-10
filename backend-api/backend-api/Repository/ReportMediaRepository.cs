using backend_api.Data;
using backend_api.Models;
using backend_api.Repository.IRepository;

namespace backend_api.Repository
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
