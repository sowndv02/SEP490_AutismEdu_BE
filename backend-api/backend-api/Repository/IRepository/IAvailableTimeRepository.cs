using backend_api.Models;

namespace backend_api.Repository.IRepository
{
    public interface IAvailableTimeRepository : IRepository<AvailableTime>
    {
        Task<AvailableTime> UpdateAsync(AvailableTime model);
    }
}
