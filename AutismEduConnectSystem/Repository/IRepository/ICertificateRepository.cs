
using AutismEduConnectSystem.Models;

namespace AutismEduConnectSystem.Repository.IRepository
{
    public interface ICertificateRepository : IRepository<Certificate>
    {
        Task<Certificate> UpdateAsync(Certificate model);
    }
}
