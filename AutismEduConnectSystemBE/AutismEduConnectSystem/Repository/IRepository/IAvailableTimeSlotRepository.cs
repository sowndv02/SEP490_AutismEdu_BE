using AutismEduConnectSystem.Models;

namespace AutismEduConnectSystem.Repository.IRepository
{
    public interface IAvailableTimeSlotRepository : IRepository<AvailableTimeSlot>
    {
        Task<AvailableTimeSlot> UpdateAsync(AvailableTimeSlot model);
    }
}
