using AutismEduConnectSystem.Data;
using AutismEduConnectSystem.Models;
using AutismEduConnectSystem.Repository.IRepository;
using Google;

namespace AutismEduConnectSystem.Repository
{
    public class AssessmentScoreRangeRepository : Repository<AssessmentScoreRange>, IAssessmentScoreRangeRepository
    {
        private readonly ApplicationDbContext _context;

        public AssessmentScoreRangeRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<AssessmentScoreRange> UpdateAsync(AssessmentScoreRange model)
        {
            try
            {
                _context.AssessmentScoreRanges.Update(model);
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
