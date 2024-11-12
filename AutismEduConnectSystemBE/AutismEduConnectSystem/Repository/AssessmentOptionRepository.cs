using AutismEduConnectSystem.Data;
using AutismEduConnectSystem.Models;
using AutismEduConnectSystem.Repository.IRepository;

namespace AutismEduConnectSystem.Repository
{
    public class AssessmentOptionRepository : Repository<AssessmentOption>, IAssessmentOptionRepository
    {
        private readonly ApplicationDbContext _context;

        public AssessmentOptionRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<AssessmentOption> UpdateAsync(AssessmentOption model)
        {
            try
            {
                _context.AssessmentOptions.Update(model);
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
