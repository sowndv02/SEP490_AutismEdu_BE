using AutismEduConnectSystem.Models;

namespace AutismEduConnectSystem.Repository.IRepository
{
    public interface IAssessmentResultRepository : IRepository<AssessmentResult>
    {
        Task<AssessmentResult> UpdateAsync(AssessmentResult model);
    }
}
