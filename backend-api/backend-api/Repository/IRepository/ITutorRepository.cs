using backend_api.Models;
using System.Linq.Expressions;

namespace backend_api.Repository.IRepository
{
    public interface ITutorRepository : IRepository<Tutor>
    {
        Task<Tutor> UpdateAsync(Tutor tutor);
        Task<(int TotalCount, List<Tutor> list)> GetAllTutorAsync(Expression<Func<Tutor, bool>>? filter = null, Expression<Func<Tutor, bool>>? filterAddress = null, int? reviewScore = 5, 
            int? ageFrom = 0, int? ageTo = 15, string? includeProperties = null,
            int pageSize = 10, int pageNumber = 1);
    }
}
