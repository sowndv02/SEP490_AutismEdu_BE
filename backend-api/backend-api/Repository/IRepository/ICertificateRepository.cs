
using backend_api.Models;

namespace backend_api.Repository.IRepository
{
    public interface ICertificateRepository : IRepository<Certificate>
    {
        Task<Certificate> UpdateAsync(Certificate model);
    }
}
