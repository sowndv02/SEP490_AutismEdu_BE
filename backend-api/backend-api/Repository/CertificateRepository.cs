using backend_api.Data;
using backend_api.Models;
using backend_api.Repository.IRepository;
using Microsoft.EntityFrameworkCore;

namespace backend_api.Repository
{
    public class CertificateRepository : Repository<Certificate>, ICertificateRepository
    {
        private readonly ApplicationDbContext _context;
        public CertificateRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }
        
        public async Task<Certificate> UpdateAsync(Certificate model)
        {
            try
            {
                _context.Certificates.Update(model);
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
