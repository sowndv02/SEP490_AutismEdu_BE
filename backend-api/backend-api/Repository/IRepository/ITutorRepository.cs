using backend_api.Models;
using System.Linq.Expressions;

namespace backend_api.Repository.IRepository
{
    public interface ITutorRepository : IRepository<Tutor>
    {
        Task<Tutor> UpdateAsync(Tutor tutor);
        Task<(int TotalCount, List<Tutor> list)> GetAllTutorAsync(Expression<Func<Tutor, bool>>? filterName = null, Expression<Func<Tutor, bool>>? filterAddress = null, int? filterScore = null, Expression<Func<Tutor, bool>>? filterAge = null, string? includeProperties = null,
            int pageSize = 0, int pageNumber = 1, Expression<Func<Tutor, object>>? orderBy = null, bool isDesc = true);
    }
}
