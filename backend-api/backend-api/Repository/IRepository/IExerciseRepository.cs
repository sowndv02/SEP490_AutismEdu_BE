using backend_api.Models;

namespace backend_api.Repository.IRepository
{
    public interface IExerciseRepository : IRepository<Exercise>
    {
        Task<Exercise> UpdateAsync(Exercise model);
        Task<(int TotalCount, List<Exercise>)> GetByExerciseTypeAsync(int exerciseTypeId);
    }
}