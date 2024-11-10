using backend_api.Models;

namespace backend_api.Repository.IRepository
{
    public interface IReportRepository : IRepository<Report>
    {
        Task<Report> UpdateAsync(Report model);
    }
}
