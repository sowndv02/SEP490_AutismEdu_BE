using backend_api.Models;

namespace backend_api.Repository.IRepository
{
    public interface ICurriculumRepository : IRepository<Curriculum>
    {
        Task<Curriculum> UpdateAsync(Curriculum model);
        Task<int> GetNextVersionNumberAsync(int? originalCurriculumId);
        Task DeactivatePreviousVersionsAsync(int? originalCurriculumId);
    }
}
