using backend_api.Models;

namespace backend_api.Repository.IRepository
{
    public interface IProgressReportRepository : IRepository<ProgressReport>
    {
        Task<ProgressReport> UpdateAsync(ProgressReport model);
    }
}
