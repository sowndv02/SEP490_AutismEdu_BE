using backend_api.Models;

namespace backend_api.Repository.IRepository
{
    public interface IReportMediaRepository : IRepository<ReportMedia>
    {
        Task<ReportMedia> UpdateAsync(ReportMedia model);
    }
}
