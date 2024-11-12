using AutismEduConnectSystem.Models;

namespace AutismEduConnectSystem.Repository.IRepository
{
    public interface ITutorRequestRepository : IRepository<TutorRequest>
    {
        Task<TutorRequest> UpdateAsync(TutorRequest model);
    }
}
