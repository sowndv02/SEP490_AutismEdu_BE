using backend_api.Models;

namespace backend_api.Repository.IRepository
{
    public interface IAssessmentOptionRepository : IRepository<AssessmentOption>
    {
        Task<AssessmentOption> UpdateAsync(AssessmentOption model);
    }
}
