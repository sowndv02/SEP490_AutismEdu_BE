using AutismEduConnectSystem.Models;

namespace AutismEduConnectSystem.Repository.IRepository
{
    public interface IPackagePaymentRepository : IRepository<PackagePayment>
    {
        Task<PackagePayment> UpdateAsync(PackagePayment model);
        Task<int> GetNextVersionNumberAsync(int? originalId);
        Task DeactivatePreviousVersionsAsync(int? originalId);
    }
}
