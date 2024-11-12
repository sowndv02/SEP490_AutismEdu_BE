using AutismEduConnectSystem.Models;

namespace AutismEduConnectSystem.Repository.IRepository
{
    public interface IScheduleTimeSlotRepository : IRepository<ScheduleTimeSlot>
    {
        Task<ScheduleTimeSlot> UpdateAsync(ScheduleTimeSlot model);
    }
}
