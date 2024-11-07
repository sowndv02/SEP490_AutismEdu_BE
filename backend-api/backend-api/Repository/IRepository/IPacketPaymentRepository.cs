using backend_api.Models;

namespace backend_api.Repository.IRepository
{
    public interface IPacketPaymentRepository : IRepository<PacketPayment>
    {
        Task<PacketPayment> UpdateAsync(PacketPayment model);
        Task<int> GetNextVersionNumberAsync(int? originalId);
        Task DeactivatePreviousVersionsAsync(int? originalId);
    }
}
