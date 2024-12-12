using AutismEduConnectSystem.Models;
using System.Linq.Expressions;

namespace AutismEduConnectSystem.Repository.IRepository
{
    public interface INotificationRepository : IRepository<Notification>
    {
        Task<Notification> UpdateAsync(Notification model);
        Task<int> TotalUnRead(Expression<Func<Notification, bool>>? filter = null);
    }
}
