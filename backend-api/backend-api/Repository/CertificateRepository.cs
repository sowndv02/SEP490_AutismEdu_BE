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
        public async Task DeactivatePreviousVersionsAsync(int? originalId)
        {
            var previousVersions = await _context.Certificates
                .Where(c => c.OriginalId == originalId && c.IsActive)
                .ToListAsync();

            if (previousVersions == null || !previousVersions.Any())
            {
                return;
            }

            foreach (var version in previousVersions)
            {
                version.IsActive = false;
                _context.Certificates.Update(version);
            }
            await _context.SaveChangesAsync();
        }

        public async Task<int> GetNextVersionNumberAsync(int? originalId)
        {
            var previousVersions = await _context.Certificates
                .Where(c => c.OriginalId == originalId)
                .ToListAsync();

            if (previousVersions == null || !previousVersions.Any())
            {
                return 1;
            }

            return previousVersions.Max(c => c.VersionNumber) + 1;
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
