using backend_api.Models;

namespace backend_api.Repository.IRepository
{
    public interface IAssessmentResultRepository : IRepository<AssessmentResult>
    {
        Task<AssessmentResult> UpdateAsync(AssessmentResult model);
    }
}
