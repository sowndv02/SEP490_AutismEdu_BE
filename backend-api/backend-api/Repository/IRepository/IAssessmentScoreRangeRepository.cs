using backend_api.Models;

namespace backend_api.Repository.IRepository
{
    public interface IAssessmentScoreRangeRepository : IRepository<AssessmentScoreRange>
    {
        Task<AssessmentScoreRange> UpdateAsync(AssessmentScoreRange model);
    }
}
