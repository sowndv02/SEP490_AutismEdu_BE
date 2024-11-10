using backend_api.Models;

namespace backend_api.Repository.IRepository
{
    public interface IConversationRepository : IRepository<Conversation>
    {
        Task<Conversation> UpdateAsync(Conversation model);
    }
}
