using AutismEduConnectSystem.Models;

namespace AutismEduConnectSystem.Repository.IRepository
{
    public interface IProgressReportRepository : IRepository<ProgressReport>
    {
        Task<ProgressReport> UpdateAsync(ProgressReport model);
    }
}
