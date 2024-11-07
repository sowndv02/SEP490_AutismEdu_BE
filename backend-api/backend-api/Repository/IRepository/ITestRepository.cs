using backend_api.Models;

namespace backend_api.Repository.IRepository
{
    public interface ITestRepository : IRepository<Test>
    {
        Task<Test> UpdateAsync(Test model);
    }
}
