using backend_api.Models;

namespace backend_api.Repository.IRepository
{
    public interface ITutorRepository : IRepository<Tutor>
    {
        Task<Tutor> UpdateAsync(Tutor tutor);
    }
}
