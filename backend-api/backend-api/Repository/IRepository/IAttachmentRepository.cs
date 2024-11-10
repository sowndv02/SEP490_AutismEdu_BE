using backend_api.Models;

namespace backend_api.Repository.IRepository
{
    public interface IAttachmentRepository : IRepository<Attachment>
    {
        Task<Attachment> UpdateAsync(Attachment model);
    }
}
