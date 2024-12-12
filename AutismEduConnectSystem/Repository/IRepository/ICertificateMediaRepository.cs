using AutismEduConnectSystem.Models;

namespace AutismEduConnectSystem.Repository.IRepository
{
    public interface ICertificateMediaRepository : IRepository<CertificateMedia>
    {
        Task<CertificateMedia> UpdateAsync(CertificateMedia model);
    }
}
