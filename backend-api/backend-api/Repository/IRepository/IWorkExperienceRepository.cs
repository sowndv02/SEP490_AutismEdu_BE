using backend_api.Models;

namespace backend_api.Repository.IRepository
{
    public interface IWorkExperienceRepository : IRepository<WorkExperience>
    {
        Task<WorkExperience> UpdateAsync(WorkExperience model);
    }
}
