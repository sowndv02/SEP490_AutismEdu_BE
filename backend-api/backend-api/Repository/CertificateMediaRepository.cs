using backend_api.Data;
using backend_api.Models;
using backend_api.Repository.IRepository;

namespace backend_api.Repository
{
    public class CertificateMediaRepository : Repository<CertificateMedia>, ICertificateMediaRepository
    {
        private readonly ApplicationDbContext _context;
        public CertificateMediaRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<CertificateMedia> UpdateAsync(CertificateMedia model)
        {
            try
            {
                _context.CertificateMedias.Update(model);
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
