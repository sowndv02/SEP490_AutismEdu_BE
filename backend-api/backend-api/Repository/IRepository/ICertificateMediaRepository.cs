using backend_api.Models;

namespace backend_api.Repository.IRepository
{
    public interface ICertificateMediaRepository : IRepository<CertificateMedia>
    {
        Task<CertificateMedia> UpdateAsync(CertificateMedia model);
    }
}
