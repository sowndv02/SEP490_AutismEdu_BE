using backend_api.Data;
using backend_api.Models;
using backend_api.Repository.IRepository;
using Microsoft.EntityFrameworkCore;

namespace backend_api.Repository
{
    public class CurriculumRepository : Repository<Curriculum>, ICurriculumRepository
    {
        private readonly ApplicationDbContext _context;
        public CurriculumRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task DeactivatePreviousVersionsAsync(int? originalId)
        {
            if (originalId == 0 || originalId == null) return;
            var previousVersions = await _context.Curriculums
                .Where(c => (c.OriginalId == originalId || c.Id == originalId) && c.IsActive)
                .ToListAsync();

            if (previousVersions == null || !previousVersions.Any())
            {
                return;
            }

            foreach (var version in previousVersions)
            {
                version.IsActive = false;
                _context.Curriculums.Update(version);
            }
            await _context.SaveChangesAsync();
        }

        public async Task<int> GetNextVersionNumberAsync(int? originalId)
        {
            var previousVersions = await _context.Curriculums
                .Where(c => c.OriginalCurriculumId == originalId)
                .ToListAsync();

            if (previousVersions == null || !previousVersions.Any())
            {
                return 1;
            }

            return previousVersions.Max(c => c.VersionNumber) + 1;
        }

        public async Task<Curriculum> UpdateAsync(Curriculum model)
        {
            try
            {
                _context.Curriculums.Update(model);
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
