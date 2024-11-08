using backend_api.Models;

namespace backend_api.Repository.IRepository
{
    public interface INotificationRepository : IRepository<Notification>
    {
        Task<Notification> UpdateAsync(Notification model);
    }
}
