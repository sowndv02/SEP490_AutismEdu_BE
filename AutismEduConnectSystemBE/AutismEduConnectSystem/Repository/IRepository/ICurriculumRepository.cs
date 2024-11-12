using AutismEduConnectSystem.Models;

namespace AutismEduConnectSystem.Repository.IRepository
{
    public interface ICurriculumRepository : IRepository<Curriculum>
    {
        Task<Curriculum> UpdateAsync(Curriculum model);
        Task<int> GetNextVersionNumberAsync(int? originalCurriculumId);
        Task DeactivatePreviousVersionsAsync(int? originalCurriculumId);
    }
}
