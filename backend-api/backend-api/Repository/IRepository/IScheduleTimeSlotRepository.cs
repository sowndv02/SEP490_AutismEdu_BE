using backend_api.Models;

namespace backend_api.Repository.IRepository
{
    public interface IScheduleTimeSlotRepository : IRepository<ScheduleTimeSlot>
    {
        Task<ScheduleTimeSlot> UpdateAsync(ScheduleTimeSlot model);
    }
}
