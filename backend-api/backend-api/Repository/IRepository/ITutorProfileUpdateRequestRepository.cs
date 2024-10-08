using backend_api.Models;

namespace backend_api.Repository.IRepository
{
    public interface ITutorProfileUpdateRequestRepository : IRepository<TutorProfileUpdateRequest>
    {
        Task<TutorProfileUpdateRequest> UpdateAsync(TutorProfileUpdateRequest model);
    }
}
