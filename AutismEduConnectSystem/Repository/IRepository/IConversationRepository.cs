using AutismEduConnectSystem.Models;

namespace AutismEduConnectSystem.Repository.IRepository
{
    public interface IConversationRepository : IRepository<Conversation>
    {
        Task<Conversation> UpdateAsync(Conversation model);
    }
}
