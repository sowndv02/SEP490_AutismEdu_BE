using backend_api.Models;

namespace backend_api.Repository.IRepository
{
    public interface IBlogRepository : IRepository<Blog>
    {
        Task<Blog> UpdateAsync(Blog model);
    }
}
