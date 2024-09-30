using backend_api.Models;

namespace backend_api.Repository.IRepository
{
    public interface IActivityLogRepository : IRepository<ActivityLog>
    {
        Task<ActivityLog> UpdateAsync(ActivityLog model);
    }
}
