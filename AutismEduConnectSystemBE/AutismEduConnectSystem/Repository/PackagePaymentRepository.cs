using AutismEduConnectSystem.Data;
using AutismEduConnectSystem.Models;
using AutismEduConnectSystem.Repository.IRepository;
using Microsoft.EntityFrameworkCore;

namespace AutismEduConnectSystem.Repository
{
    public class PackagePaymentRepository : Repository<PackagePayment>, IPackagePaymentRepository
    {
        private readonly ApplicationDbContext _context;
        public PackagePaymentRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }
        public async Task DeactivatePreviousVersionsAsync(int? originalId)
        {
            if (originalId == 0 || originalId == null) return;
            var previousVersions = await _context.PackagePayments
                .Where(c => (c.OriginalId == originalId || c.Id == originalId) && c.IsActive)
                .ToListAsync();

            if (previousVersions == null || !previousVersions.Any())
            {
                return;
            }

            foreach (var version in previousVersions)
            {
                version.IsActive = false;
                _context.PackagePayments.Update(version);
            }
            await _context.SaveChangesAsync();
        }

        public async Task<int> GetNextVersionNumberAsync(int? originalId)
        {
            var previousVersions = await _context.PackagePayments
                .Where(c => c.OriginalId == originalId)
                .ToListAsync();

            if (previousVersions == null || !previousVersions.Any())
            {
                return 1;
            }

            return previousVersions.Max(c => c.VersionNumber) + 1;
        }

        public async Task<PackagePayment> UpdateAsync(PackagePayment model)
        {
            try
            {
                _context.PackagePayments.Update(model);
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
