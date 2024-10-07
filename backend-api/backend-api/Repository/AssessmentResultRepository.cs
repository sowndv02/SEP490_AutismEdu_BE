using backend_api.Data;
using backend_api.Models;
using backend_api.Repository.IRepository;

namespace backend_api.Repository
{
    public class AssessmentResultRepository : Repository<AssessmentResult>, IAssessmentResultRepository
    {
        private readonly ApplicationDbContext _context;

        public AssessmentResultRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<AssessmentResult> UpdateAsync(AssessmentResult model)
        {
            try
            {
                _context.AssessmentResults.Update(model);
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
