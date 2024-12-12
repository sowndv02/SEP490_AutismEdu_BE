using AutismEduConnectSystem.Models;

namespace AutismEduConnectSystem.Repository.IRepository
{
    public interface IMessageRepository : IRepository<Message>
    {
        Task<Message> UpdateAsync(Message model);
    }
}
