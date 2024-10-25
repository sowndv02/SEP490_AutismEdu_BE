using backend_api.Models;

namespace backend_api.Repository.IRepository
{
    public interface ISyllabusRepository : IRepository<Syllabus>
    {
        Task<Syllabus> UpdateAsync(Syllabus model);
    }
}
