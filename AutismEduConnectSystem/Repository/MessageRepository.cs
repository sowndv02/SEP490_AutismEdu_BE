using AutismEduConnectSystem.Data;
using AutismEduConnectSystem.Models;
using AutismEduConnectSystem.Repository.IRepository;

namespace AutismEduConnectSystem.Repository
{
    public class MessageRepository : Repository<Message>, IMessageRepository
    {
        private readonly ApplicationDbContext _context;
        public MessageRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<Message> UpdateAsync(Message model)
        {
            try
            {
                _context.Messages.Update(model);
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
