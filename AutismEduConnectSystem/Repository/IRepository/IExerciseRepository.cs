using AutismEduConnectSystem.Models;

namespace AutismEduConnectSystem.Repository.IRepository
{
    public interface IExerciseRepository : IRepository<Exercise>
    {
        Task<Exercise> UpdateAsync(Exercise model);
        Task<int> CountByExerciseType(int exerciseTypeId);
        Task<int> GetNextVersionNumberAsync(int? originalId);
        Task DeactivatePreviousVersionsAsync(int? originalId);
    }
}