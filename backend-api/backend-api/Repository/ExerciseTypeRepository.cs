using backend_api.Data;
using backend_api.Models;
using backend_api.Repository.IRepository;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace backend_api.Repository
{
    public class ExerciseTypeRepository : Repository<ExerciseType>, IExerciseTypeRepository
    {
        private readonly ApplicationDbContext _context;
        public ExerciseTypeRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }
        public async Task DeactivatePreviousVersionsAsync(int? originalId)
        {
            if (originalId == 0 || originalId == null) return;
            var previousVersions = await _context.ExerciseTypes
                .Where(c => (c.OriginalId == originalId || c.Id == originalId) && c.IsActive)
                .ToListAsync();

            if (previousVersions == null || !previousVersions.Any())
            {
                return;
            }

            foreach (var version in previousVersions)
            {
                version.IsActive = false;
                _context.ExerciseTypes.Update(version);
            }
            await _context.SaveChangesAsync();
        }

        public async Task<int> GetNextVersionNumberAsync(int? originalCurriculumId)
        {
            var previousVersions = await _context.Curriculums
                .Where(c => c.OriginalCurriculumId == originalCurriculumId)
                .ToListAsync();

            if (previousVersions == null || !previousVersions.Any())
            {
                return 1;
            }

            return previousVersions.Max(c => c.VersionNumber) + 1;
        }
        public Task<ExerciseType> UpdateAsync(ExerciseType model)
        {
            throw new NotImplementedException();
        }
    }
}
