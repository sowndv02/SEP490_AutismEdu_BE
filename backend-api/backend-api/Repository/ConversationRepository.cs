using backend_api.Data;
using backend_api.Models;
using backend_api.Repository.IRepository;

namespace backend_api.Repository
{
    public class ConversationRepository : Repository<Conversation>, IConversationRepository
    {
        private readonly ApplicationDbContext _context;
        public ConversationRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<Conversation> UpdateAsync(Conversation model)
        {
            try
            {
                _context.Conversations.Update(model);
                await _context.SaveChangesAsync();
                return model;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
    }
}
