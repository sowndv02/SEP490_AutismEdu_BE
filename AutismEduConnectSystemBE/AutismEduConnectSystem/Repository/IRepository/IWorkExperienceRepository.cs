using AutismEduConnectSystem.Models;

namespace AutismEduConnectSystem.Repository.IRepository
{
    public interface IWorkExperienceRepository : IRepository<WorkExperience>
    {
        Task<WorkExperience> UpdateAsync(WorkExperience model);
        Task<int> GetNextVersionNumberAsync(int? originalId);
        Task DeactivatePreviousVersionsAsync(int? originalId);
    }
}
