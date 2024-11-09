using backend_api.Models;
using System.Linq.Expressions;

namespace backend_api.Repository.IRepository
{
    public interface INotificationRepository : IRepository<Notification>
    {
        Task<Notification> UpdateAsync(Notification model);
        Task<int> TotalUnRead(Expression<Func<Notification, bool>>? filter = null);
    }
}
