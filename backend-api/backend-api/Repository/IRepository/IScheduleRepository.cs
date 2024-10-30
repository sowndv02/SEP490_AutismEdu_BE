using backend_api.Models;

namespace backend_api.Repository.IRepository
{
    public interface IScheduleRepository : IRepository<Schedule>
    {
        Task<Schedule> UpdateAsync(Schedule model);
    }
}
