using AutismEduConnectSystem.Models;

namespace AutismEduConnectSystem.Repository.IRepository
{
    public interface IExerciseTypeRepository : IRepository<ExerciseType>
    {
        Task<ExerciseType> UpdateAsync(ExerciseType model);
        Task<int> GetNextVersionNumberAsync(int? originalCurriculumId);
        Task DeactivatePreviousVersionsAsync(int? originalCurriculumId);
    }
}
