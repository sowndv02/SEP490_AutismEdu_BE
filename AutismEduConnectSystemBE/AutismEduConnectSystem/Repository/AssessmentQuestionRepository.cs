using AutismEduConnectSystem.Data;
using AutismEduConnectSystem.Models;
using AutismEduConnectSystem.Repository.IRepository;
using Microsoft.EntityFrameworkCore;

namespace AutismEduConnectSystem.Repository
{
    public class AssessmentQuestionRepository : Repository<AssessmentQuestion>, IAssessmentQuestionRepository
    {
        private readonly ApplicationDbContext _context;

        public AssessmentQuestionRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<AssessmentQuestion> UpdateAsync(AssessmentQuestion model)
        {
            try
            {
                _context.AssessmentQuestions.Update(model);
                await _context.SaveChangesAsync();
                return model;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public async Task DeactivatePreviousVersionsAsync(int? originalId)
        {
            if (originalId == 0 || originalId == null) return;
            var previousVersions = await _context.AssessmentQuestions
                .Where(c => (c.OriginalId == originalId || c.Id == originalId) && c.IsActive)
                .ToListAsync();

            if (previousVersions == null || !previousVersions.Any())
            {
                return;
            }

            foreach (var version in previousVersions)
            {
                version.IsActive = false;
                _context.AssessmentQuestions.Update(version);
            }
            await _context.SaveChangesAsync();
        }

        public async Task<int> GetNextVersionNumberAsync(int? originalId)
        {
            var previousVersions = await _context.AssessmentQuestions
                .Where(c => c.OriginalId == originalId)
                .ToListAsync();

            if (previousVersions == null || !previousVersions.Any())
            {
                return 1;
            }

            return previousVersions.Max(c => c.VersionNumber) + 1;
        }
    }
}
