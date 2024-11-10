using backend_api.Data;
using backend_api.Models;
using backend_api.Repository.IRepository;

namespace backend_api.Repository
{
    public class AttachmentRepository : Repository<Attachment>, IAttachmentRepository
    {
        private readonly ApplicationDbContext _context;
        public AttachmentRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<Attachment> UpdateAsync(Attachment model)
        {
            try
            {
                _context.Attachments.Update(model);
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
