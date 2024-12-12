using AutismEduConnectSystem.Models;

namespace AutismEduConnectSystem.Repository.IRepository
{
    public interface IScheduleRepository : IRepository<Schedule>
    {
        Task<Schedule> UpdateAsync(Schedule model);
    }
}
