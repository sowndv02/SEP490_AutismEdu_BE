using AutismEduConnectSystem.Models;

namespace AutismEduConnectSystem.Repository.IRepository
{
    public interface IAttachmentRepository : IRepository<Attachment>
    {
        Task<Attachment> UpdateAsync(Attachment model);
    }
}
