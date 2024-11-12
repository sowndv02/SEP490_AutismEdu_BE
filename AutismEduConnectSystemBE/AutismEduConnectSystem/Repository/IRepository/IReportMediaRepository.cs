using AutismEduConnectSystem.Models;

namespace AutismEduConnectSystem.Repository.IRepository
{
    public interface IReportMediaRepository : IRepository<ReportMedia>
    {
        Task<ReportMedia> UpdateAsync(ReportMedia model);
    }
}
