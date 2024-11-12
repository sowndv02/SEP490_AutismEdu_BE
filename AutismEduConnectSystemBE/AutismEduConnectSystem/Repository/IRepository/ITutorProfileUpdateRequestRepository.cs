using AutismEduConnectSystem.Models;

namespace AutismEduConnectSystem.Repository.IRepository
{
    public interface ITutorProfileUpdateRequestRepository : IRepository<TutorProfileUpdateRequest>
    {
        Task<TutorProfileUpdateRequest> UpdateAsync(TutorProfileUpdateRequest model);
    }
}
