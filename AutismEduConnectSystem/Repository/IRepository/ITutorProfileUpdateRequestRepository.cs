using AutismEduConnectSystem.Models;
using System.Linq.Expressions;

namespace AutismEduConnectSystem.Repository.IRepository
{
    public interface ITutorProfileUpdateRequestRepository : IRepository<TutorProfileUpdateRequest>
    {
        Task<TutorProfileUpdateRequest> UpdateAsync(TutorProfileUpdateRequest model);
        Task<(int TotalCount, List<TutorProfileUpdateRequest> list)> GetAllTutorUpdateRequestAsync(Expression<Func<TutorProfileUpdateRequest, bool>>? filterName = null, Expression<Func<TutorProfileUpdateRequest, bool>>? filterOther = null, string? includeProperties = null,
          int pageSize = 0, int pageNumber = 1, Expression<Func<TutorProfileUpdateRequest, object>>? orderBy = null, bool isDesc = true);
    }
}
