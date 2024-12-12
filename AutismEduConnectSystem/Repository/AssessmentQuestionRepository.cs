using AutismEduConnectSystem.Data;
using AutismEduConnectSystem.Models;
using AutismEduConnectSystem.Repository.IRepository;

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
    }
}
