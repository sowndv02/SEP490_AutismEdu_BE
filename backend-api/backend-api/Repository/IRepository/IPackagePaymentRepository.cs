using backend_api.Models;

namespace backend_api.Repository.IRepository
{
    public interface IPackagePaymentRepository : IRepository<PackagePayment>
    {
        Task<PackagePayment> UpdateAsync(PackagePayment model);
        Task<int> GetNextVersionNumberAsync(int? originalId);
        Task DeactivatePreviousVersionsAsync(int? originalId);
    }
}
