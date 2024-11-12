using AutismEduConnectSystem.Models;

namespace AutismEduConnectSystem.Repository.IRepository
{
    public interface ITestRepository : IRepository<Test>
    {
        Task<Test> UpdateAsync(Test model);
    }
}
