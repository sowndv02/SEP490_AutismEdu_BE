using System.Linq.Expressions;

namespace AutismEduConnectSystem.Repository.IRepository
{
    public interface IRepository<T> where T : class
    {
        Task<(int TotalCount, List<T> list)> GetAllAsync(Expression<Func<T, bool>>? filter = null, string? includeProperties = null,
            int pageSize = 10, int pageNumber = 1, Expression<Func<T, object>>? orderBy = null, bool isDesc = true);
        Task<(int TotalCount, List<T> list)> GetAllWithIncludeAsync(Expression<Func<T, bool>>? filter = null, string? includeProperties = null,
            int pageSize = 10, int pageNumber = 1, Expression<Func<T, object>>? orderBy = null, bool isDesc = true);
        Task<(int TotalCount, List<T> list)> GetAllNotPagingAsync(Expression<Func<T, bool>>? filter = null, string? includeProperties = null, string? excludeProperties = null, Expression<Func<T, object>>? orderBy = null, bool isDesc = true);
        Task<T> GetAsync(Expression<Func<T, bool>> filter = null, bool tracked = true, string? includeProperties = null, string? excludeProperties = null);
        Task<T> CreateAsync(T entity);
        Task RemoveAsync(T entity);
        Task SaveAsync();
    }
}
