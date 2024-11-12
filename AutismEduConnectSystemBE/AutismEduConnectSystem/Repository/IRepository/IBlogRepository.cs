using AutismEduConnectSystem.Models;

namespace AutismEduConnectSystem.Repository.IRepository
{
    public interface IBlogRepository : IRepository<Blog>
    {
        Task<Blog> UpdateAsync(Blog model);
    }
}
