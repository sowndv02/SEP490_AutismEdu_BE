
using backend_api.Models;

namespace backend_api.Repository.IRepository
{
    public interface ILicenseRepository : IRepository<Licence>
    {
        Task<Licence> UpdateAsync(Licence model);
    }
}
