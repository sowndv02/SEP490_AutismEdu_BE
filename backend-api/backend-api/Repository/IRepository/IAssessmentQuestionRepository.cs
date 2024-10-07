using backend_api.Models;

namespace backend_api.Repository.IRepository
{
    public interface IAssessmentQuestionRepository : IRepository<AssessmentQuestion>
    {
        Task<AssessmentQuestion> UpdateAsync(AssessmentQuestion model);
    }
}
