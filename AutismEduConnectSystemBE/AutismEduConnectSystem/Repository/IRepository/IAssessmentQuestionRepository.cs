using AutismEduConnectSystem.Models;

namespace AutismEduConnectSystem.Repository.IRepository
{
    public interface IAssessmentQuestionRepository : IRepository<AssessmentQuestion>
    {
        Task<AssessmentQuestion> UpdateAsync(AssessmentQuestion model);
        Task<int> GetNextVersionNumberAsync(int? originalId);
        Task DeactivatePreviousVersionsAsync(int? originalId);
    }
}
