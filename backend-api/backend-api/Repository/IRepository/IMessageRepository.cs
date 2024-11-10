using backend_api.Models;

namespace backend_api.Repository.IRepository
{
    public interface IMessageRepository : IRepository<Message>
    {
        Task<Message> UpdateAsync(Message model);
    }
}
