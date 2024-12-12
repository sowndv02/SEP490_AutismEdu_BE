using AutismEduConnectSystem.Models;

namespace AutismEduConnectSystem.Repository.IRepository
{
    public interface IAssessmentScoreRangeRepository : IRepository<AssessmentScoreRange>
    {
        Task<AssessmentScoreRange> UpdateAsync(AssessmentScoreRange model);
    }
}
