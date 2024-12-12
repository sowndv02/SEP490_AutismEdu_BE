using AutismEduConnectSystem.Models;

namespace AutismEduConnectSystem.Repository.IRepository
{
    public interface IActivityLogRepository : IRepository<ActivityLog>
    {
        Task<ActivityLog> UpdateAsync(ActivityLog model);
    }
}
