using backend_api.Models;

namespace backend_api.Repository.IRepository
{
    public interface IAvailableTimeSlotRepository : IRepository<AvailableTimeSlot>
    {
        Task<AvailableTimeSlot> UpdateAsync(AvailableTimeSlot model);
    }
}
