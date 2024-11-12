using AutismEduConnectSystem.Models;

namespace AutismEduConnectSystem.Repository.IRepository
{
    public interface ITutorRegistrationRequestRepository : IRepository<TutorRegistrationRequest>
    {
        Task<TutorRegistrationRequest> UpdateAsync(TutorRegistrationRequest model);
    }
}
