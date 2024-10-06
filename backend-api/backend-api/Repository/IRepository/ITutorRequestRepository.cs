using backend_api.Models;

namespace backend_api.Repository.IRepository
{
    public interface ITutorRequestRepository : IRepository<TutorRequest>
    {
        Task<TutorRequest> UpdateAsync(TutorRequest model);
    }
}
