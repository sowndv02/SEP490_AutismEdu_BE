using AutismEduConnectSystem.Models;

namespace AutismEduConnectSystem.Repository.IRepository
{
    public interface IReportRepository : IRepository<Report>
    {
        Task<Report> UpdateAsync(Report model);
    }
}
