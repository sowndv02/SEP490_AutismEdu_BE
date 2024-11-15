using AutismEduConnectSystem.Models;

namespace AutismEduConnectSystem.Repository.IRepository
{
    public interface IAssessmentOptionRepository : IRepository<AssessmentOption>
    {
        Task<AssessmentOption> UpdateAsync(AssessmentOption model);
        Task<int> GetNextVersionNumberAsync(int? originalId);
        Task DeactivatePreviousVersionsAsync(int? originalId);
    }
}
