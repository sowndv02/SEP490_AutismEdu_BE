using backend_api.Models;

namespace backend_api.Repository.IRepository
{
    public interface ITutorRegistrationRequestRepository : IRepository<TutorRegistrationRequest>
    {
        Task<TutorRegistrationRequest> UpdateAsync(TutorRegistrationRequest model);
    }
}
